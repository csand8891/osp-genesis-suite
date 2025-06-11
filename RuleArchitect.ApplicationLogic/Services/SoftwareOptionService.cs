using RuleArchitect.Abstractions.DTOs.SoftwareOption;
using RuleArchitect.Abstractions.DTOs.Lookups;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.Data;
using RuleArchitect.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.Services
{
    /// <summary>
    /// Service for managing Software Options, including creation, retrieval, updates, and deletion.
    /// Implements ISoftwareOptionService.
    /// </summary>
    public class SoftwareOptionService : ISoftwareOptionService
    {
        private readonly RuleArchitectContext _context;
        private readonly IUserActivityLogService _activityLogService;

        public SoftwareOptionService(RuleArchitectContext context, IUserActivityLogService activityLogService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
        }

        /// <summary>
        /// Retrieves all software options.
        /// </summary>
        /// <returns>A list of all SoftwareOption DTOs.</returns>
        public async Task<List<SoftwareOptionDto>> GetAllSoftwareOptionsAsync()
        {
            // FIX: The projection from the SoftwareOption entity to the SoftwareOptionDto
            // is now done directly inside the Select statement. This allows Entity Framework Core
            // to translate the query into efficient SQL.
            return await _context.SoftwareOptions
                                 .AsNoTracking()
                                 .Include(so => so.ControlSystem)
                                 .Select(so => new SoftwareOptionDto
                                 {
                                     SoftwareOptionId = so.SoftwareOptionId,
                                     PrimaryName = so.PrimaryName,
                                     AlternativeNames = so.AlternativeNames,
                                     SourceFileName = so.SourceFileName,
                                     PrimaryOptionNumberDisplay = so.PrimaryOptionNumberDisplay,
                                     Notes = so.Notes,
                                     ControlSystemId = so.ControlSystemId.GetValueOrDefault(),
                                     ControlSystemName = so.ControlSystem != null ? so.ControlSystem.Name : null,
                                     Version = so.Version,
                                     LastModifiedDate = so.LastModifiedDate,
                                     LastModifiedBy = so.LastModifiedBy
                                 })
                                 .ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific software option by its ID, including related data.
        /// </summary>
        /// <param name="softwareOptionId">The ID of the software option to retrieve.</param>
        /// <returns>The SoftwareOption DTO with its related data, or null if not found.</returns>
        public async Task<SoftwareOptionDetailDto?> GetSoftwareOptionByIdAsync(int softwareOptionId)
        {
            var entity = await _context.SoftwareOptions
                .AsNoTracking()
                .Include(so => so.ControlSystem)
                .Include(so => so.OptionNumberRegistries)
                .Include(so => so.Requirements)
                .Include(so => so.SoftwareOptionActivationRules)
                .Include(so => so.SoftwareOptionSpecificationCodes)
                    .ThenInclude(sosc => sosc.SpecCodeDefinition)
                .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId);

            if (entity == null)
            {
                return null;
            }

            return MapEntityToDetailDto(entity);
        }

        public async Task<SoftwareOptionDto?> CreateSoftwareOptionAsync(CreateSoftwareOptionCommandDto command, int currentUserId, string currentUserName)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command.PrimaryName)) throw new ArgumentException("PrimaryName is required.", nameof(command.PrimaryName));
            if (!command.ControlSystemId.HasValue || command.ControlSystemId.Value <= 0) throw new ArgumentException("Valid ControlSystemId is required.", nameof(command.ControlSystemId));

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var newSoftwareOption = new SoftwareOption
                    {
                        PrimaryName = command.PrimaryName,
                        AlternativeNames = command.AlternativeNames,
                        SourceFileName = command.SourceFileName,
                        PrimaryOptionNumberDisplay = command.PrimaryOptionNumberDisplay,
                        Notes = command.Notes,
                        ControlSystemId = command.ControlSystemId.Value,
                        Version = 1,
                        LastModifiedDate = DateTime.UtcNow,
                        LastModifiedBy = currentUserName
                    };

                    _context.SoftwareOptions.Add(newSoftwareOption);
                    await _context.SaveChangesAsync();
                    var controlSystemName = await _context.ControlSystems
                        .Where(cs => cs.ControlSystemId == newSoftwareOption.ControlSystemId)
                        .Select(cs => cs.Name)
                        .FirstOrDefaultAsync() ?? "Unknown";

                    await _activityLogService.LogActivityAsync(
                        userId: currentUserId,
                        userName: currentUserName,
                        activityType: "CreateSoftwareOption",
                        description: $"User '{currentUserName}' created new software option '{newSoftwareOption.PrimaryName} ({controlSystemName})'.",
                        targetEntityType: "SoftwareOption",
                        targetEntityId: newSoftwareOption.SoftwareOptionId, // Now we have the ID
                        targetEntityDescription: newSoftwareOption.PrimaryName,
                        saveChanges: false // Let the transaction handle the final save
                    );
                    await ProcessOptionNumbersAsync(newSoftwareOption, command.OptionNumbers);
                    await ProcessRequirementsAsync(newSoftwareOption, command.Requirements);
                    await ProcessActivationRulesAsync(newSoftwareOption, command.ActivationRules);
                    await ProcessSpecificationCodesAsync(newSoftwareOption, command.SpecificationCodes);

                    _context.SoftwareOptionHistories.Add(CreateHistoryFromOption(newSoftwareOption));

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return await GetSoftwareOptionByIdAsync(newSoftwareOption.SoftwareOptionId);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<SoftwareOptionDto?> UpdateSoftwareOptionAsync(UpdateSoftwareOptionCommandDto command, int currentUserId, string currentUserName)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var softwareOptionToUpdate = await _context.SoftwareOptions
                        .Include(so => so.ControlSystem)
                        .Include(so => so.OptionNumberRegistries)
                        .Include(so => so.Requirements)
                        .Include(so => so.SoftwareOptionActivationRules)
                        .Include(so => so.SoftwareOptionSpecificationCodes)
                        .FirstOrDefaultAsync(so => so.SoftwareOptionId == command.SoftwareOptionId);

                    if (softwareOptionToUpdate == null) return null;

                    bool isModified = false;
                    if (softwareOptionToUpdate.PrimaryName != command.PrimaryName) { softwareOptionToUpdate.PrimaryName = command.PrimaryName; isModified = true; }
                    if (softwareOptionToUpdate.AlternativeNames != command.AlternativeNames) { softwareOptionToUpdate.AlternativeNames = command.AlternativeNames; isModified = true; }
                    if (softwareOptionToUpdate.SourceFileName != command.SourceFileName) { softwareOptionToUpdate.SourceFileName = command.SourceFileName; isModified = true; }
                    if (softwareOptionToUpdate.PrimaryOptionNumberDisplay != command.PrimaryOptionNumberDisplay) { softwareOptionToUpdate.PrimaryOptionNumberDisplay = command.PrimaryOptionNumberDisplay; isModified = true; }
                    if (softwareOptionToUpdate.Notes != command.Notes) { softwareOptionToUpdate.Notes = command.Notes; isModified = true; }
                    if (softwareOptionToUpdate.ControlSystemId != command.ControlSystemId) { softwareOptionToUpdate.ControlSystemId = command.ControlSystemId; isModified = true; }
                    if (softwareOptionToUpdate.CheckedBy != command.CheckedBy) { softwareOptionToUpdate.CheckedBy = command.CheckedBy; isModified = true; }
                    if (softwareOptionToUpdate.CheckedDate != command.CheckedDate) { softwareOptionToUpdate.CheckedDate = command.CheckedDate; isModified = true; }

                    if (command.OptionNumbers != null) { await ProcessOptionNumbersAsync(softwareOptionToUpdate, command.OptionNumbers); isModified = true; }
                    if (command.Requirements != null) { await ProcessRequirementsAsync(softwareOptionToUpdate, command.Requirements); isModified = true; }
                    if (command.ActivationRules != null) { await ProcessActivationRulesAsync(softwareOptionToUpdate, command.ActivationRules); isModified = true; }
                    if (command.SpecificationCodes != null) { await ProcessSpecificationCodesAsync(softwareOptionToUpdate, command.SpecificationCodes); isModified = true; }

                    if (isModified)
                    {
                        softwareOptionToUpdate.Version += 1;
                        softwareOptionToUpdate.LastModifiedDate = DateTime.UtcNow;
                        softwareOptionToUpdate.LastModifiedBy = currentUserName;
                        await _activityLogService.LogActivityAsync(
                            userId: currentUserId,
                            userName: currentUserName,
                            activityType: "UpdateSoftwareOption",
                            // Updated description string
                            description: $"User '{currentUserName}' updated software option '{softwareOptionToUpdate.PrimaryName} ({softwareOptionToUpdate.ControlSystem?.Name ?? "Unknown"})'.",
                            targetEntityType: "SoftwareOption",
                            targetEntityId: softwareOptionToUpdate.SoftwareOptionId,
                            targetEntityDescription: softwareOptionToUpdate.PrimaryName,
                            saveChanges: false
                        );
                        _context.SoftwareOptionHistories.Add(CreateHistoryFromOption(softwareOptionToUpdate));
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return await GetSoftwareOptionByIdAsync(softwareOptionToUpdate.SoftwareOptionId);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<List<SoftwareOptionHistoryDto>> GetSoftwareOptionHistoryAsync(int softwareOptionId)
        {
            return await _context.SoftwareOptionHistories
                .AsNoTracking()
                .Where(h => h.SoftwareOptionId == softwareOptionId)
                .OrderByDescending(h => h.Version)
                .Select(h => new SoftwareOptionHistoryDto
                {
                    SoftwareOptionHistoryId = h.SoftwareOptionHistoryId,
                    SoftwareOptionId = h.SoftwareOptionId,
                    Version = h.Version,
                    PrimaryName = h.PrimaryName,
                    AlternativeNames = h.AlternativeNames,
                    SourceFileName = h.SourceFileName,
                    Notes = h.Notes,
                    ControlSystemId = h.ControlSystemId,
                    ChangeTimestamp = h.ChangeTimestamp,
                    ChangedBy = h.ChangedBy
                })
                .ToListAsync();
        }

        public async Task<bool> DeleteSoftwareOptionAsync(int softwareOptionId, int currentUserId, string currentUserName)
        {
            var softwareOptionToDelete = await _context.SoftwareOptions
                .Include(so => so.ControlSystem)
                .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId);

            if (softwareOptionToDelete == null) return false;

            await _activityLogService.LogActivityAsync(
                userId: currentUserId,
                userName: currentUserName,
                activityType: "DeleteSoftwareOption",
                // Updated description string
                description: $"User '{currentUserName}' deleted software option '{softwareOptionToDelete.PrimaryName} ({softwareOptionToDelete.ControlSystem?.Name ?? "Unknown"})'.",
                targetEntityType: "SoftwareOption",
                targetEntityId: softwareOptionToDelete.SoftwareOptionId,
                targetEntityDescription: softwareOptionToDelete.PrimaryName,
                saveChanges: false
            );

            _context.SoftwareOptions.Remove(softwareOptionToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ControlSystemLookupDto>> GetControlSystemLookupsAsync()
        {
            return await _context.ControlSystems
               .AsNoTracking()
               .Select(cs => new ControlSystemLookupDto { ControlSystemId = cs.ControlSystemId, Name = cs.Name })
               .OrderBy(cs => cs.Name)
               .ToListAsync();
        }

        public async Task<List<SpecCodeDefinitionDetailDto>> GetSpecCodeDefinitionsForControlSystemAsync(int controlSystemId)
        {
            if (controlSystemId <= 0) return new List<SpecCodeDefinitionDetailDto>();

            return await _context.SpecCodeDefinitions
                .AsNoTracking()
                .Where(scd => scd.ControlSystemId == controlSystemId)
                .Select(scd => new SpecCodeDefinitionDetailDto
                {
                    SpecCodeDefinitionId = scd.SpecCodeDefinitionId,
                    ControlSystemId = scd.ControlSystemId,
                    ControlSystemName = scd.ControlSystem.Name,
                    Category = scd.Category,
                    SpecCodeNo = scd.SpecCodeNo,
                    SpecCodeBit = scd.SpecCodeBit,
                    Description = scd.Description
                })
                .OrderBy(dto => dto.Category)
                .ThenBy(dto => dto.SpecCodeNo)
                .ThenBy(dto => dto.SpecCodeBit)
                .ToListAsync();
        }

        public async Task<SpecCodeDefinitionDetailDto?> FindSpecCodeDefinitionAsync(int controlSystemId, string category, string specCodeNo, string specCodeBit)
        {
            if (controlSystemId <= 0 || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(specCodeNo) || string.IsNullOrWhiteSpace(specCodeBit))
            {
                return null;
            }

            var specCodeDefinition = await _context.SpecCodeDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(scd =>
                    scd.ControlSystemId == controlSystemId &&
                    scd.Category == category &&
                    scd.SpecCodeNo == specCodeNo &&
                    scd.SpecCodeBit == specCodeBit);

            if (specCodeDefinition == null) return null;

            return new SpecCodeDefinitionDetailDto
            {
                SpecCodeDefinitionId = specCodeDefinition.SpecCodeDefinitionId,
                ControlSystemId = specCodeDefinition.ControlSystemId,
                ControlSystemName = (await _context.ControlSystems.FindAsync(specCodeDefinition.ControlSystemId))?.Name,
                Category = specCodeDefinition.Category,
                SpecCodeNo = specCodeDefinition.SpecCodeNo,
                SpecCodeBit = specCodeDefinition.SpecCodeBit,
                Description = specCodeDefinition.Description
            };
        }

        #region Private Helper Methods

        // This helper is now only used for single-item mapping, which is fine.
        private SoftwareOptionDto MapSoftwareOptionToDto(SoftwareOption entity)
        {
            return new SoftwareOptionDto
            {
                SoftwareOptionId = entity.SoftwareOptionId,
                PrimaryName = entity.PrimaryName,
                AlternativeNames = entity.AlternativeNames,
                SourceFileName = entity.SourceFileName,
                PrimaryOptionNumberDisplay = entity.PrimaryOptionNumberDisplay,
                Notes = entity.Notes,
                ControlSystemId = entity.ControlSystemId.GetValueOrDefault(),
                ControlSystemName = entity.ControlSystem?.Name,
                Version = entity.Version,
                LastModifiedDate = entity.LastModifiedDate,
                LastModifiedBy = entity.LastModifiedBy
            };
        }

        private SoftwareOptionDetailDto MapEntityToDetailDto(SoftwareOption entity)
        {
            return new SoftwareOptionDetailDto
            {
                SoftwareOptionId = entity.SoftwareOptionId,
                PrimaryName = entity.PrimaryName,
                AlternativeNames = entity.AlternativeNames,
                SourceFileName = entity.SourceFileName,
                PrimaryOptionNumberDisplay = entity.PrimaryOptionNumberDisplay,
                Notes = entity.Notes,
                ControlSystemId = entity.ControlSystemId.GetValueOrDefault(),
                ControlSystemName = entity.ControlSystem?.Name,
                Version = entity.Version,
                LastModifiedDate = entity.LastModifiedDate,
                LastModifiedBy = entity.LastModifiedBy,
                OptionNumbers = entity.OptionNumberRegistries.Select(onr => new OptionNumberRegistryCreateDto { OptionNumber = onr.OptionNumber }).ToList(),
                Requirements = entity.Requirements.Select(r => new RequirementCreateDto { RequirementType = r.RequirementType, Condition = r.Condition, GeneralRequiredValue = r.GeneralRequiredValue, RequiredSoftwareOptionId = r.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = r.RequiredSpecCodeDefinitionId, OspFileName = r.OspFileName, OspFileVersion = r.OspFileVersion, Notes = r.Notes }).ToList(),
                ActivationRules = entity.SoftwareOptionActivationRules.Select(ar => new SoftwareOptionActivationRuleCreateDto { RuleName = ar.RuleName, ActivationSetting = ar.ActivationSetting, Notes = ar.Notes }).ToList(),
                SpecificationCodes = entity.SoftwareOptionSpecificationCodes.Select(sosc => new SoftwareOptionSpecificationCodeCreateDto { Category = sosc.SpecCodeDefinition.Category, SpecCodeNo = sosc.SpecCodeDefinition.SpecCodeNo, SpecCodeBit = sosc.SpecCodeDefinition.SpecCodeBit, Description = sosc.SpecCodeDefinition.Description, SoftwareOptionActivationRuleId = sosc.SoftwareOptionActivationRuleId, SpecificInterpretation = sosc.SpecificInterpretation }).ToList()
            };
        }

        private SoftwareOptionHistory CreateHistoryFromOption(SoftwareOption option)
        {
            return new SoftwareOptionHistory { SoftwareOptionId = option.SoftwareOptionId, Version = option.Version, PrimaryName = option.PrimaryName, AlternativeNames = option.AlternativeNames, SourceFileName = option.SourceFileName, PrimaryOptionNumberDisplay = option.PrimaryOptionNumberDisplay, Notes = option.Notes, CheckedBy = option.CheckedBy, CheckedDate = option.CheckedDate, ControlSystemId = option.ControlSystemId, ChangeTimestamp = option.LastModifiedDate, ChangedBy = option.LastModifiedBy };
        }

        private async Task ProcessOptionNumbersAsync(SoftwareOption option, List<OptionNumberRegistryCreateDto> dtos)
        {
            option.OptionNumberRegistries ??= new List<OptionNumberRegistry>();
            _context.OptionNumberRegistries.RemoveRange(option.OptionNumberRegistries);
            if (dtos != null)
            {
                foreach (var dto in dtos) option.OptionNumberRegistries.Add(new OptionNumberRegistry { OptionNumber = dto.OptionNumber });
            }
        }

        private async Task ProcessRequirementsAsync(SoftwareOption option, List<RequirementCreateDto> dtos)
        {
            option.Requirements ??= new List<Requirement>();
            _context.Requirements.RemoveRange(option.Requirements);
            if (dtos != null)
            {
                foreach (var dto in dtos) option.Requirements.Add(new Requirement { RequirementType = dto.RequirementType, Condition = dto.Condition, GeneralRequiredValue = dto.GeneralRequiredValue ?? string.Empty, RequiredSoftwareOptionId = dto.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = dto.RequiredSpecCodeDefinitionId, OspFileName = dto.OspFileName, OspFileVersion = dto.OspFileVersion, Notes = dto.Notes });
            }
        }

        private async Task ProcessActivationRulesAsync(SoftwareOption option, List<SoftwareOptionActivationRuleCreateDto> dtos)
        {
            option.SoftwareOptionActivationRules ??= new List<SoftwareOptionActivationRule>();
            _context.SoftwareOptionActivationRules.RemoveRange(option.SoftwareOptionActivationRules);
            if (dtos != null)
            {
                foreach (var dto in dtos) option.SoftwareOptionActivationRules.Add(new SoftwareOptionActivationRule { RuleName = dto.RuleName, ActivationSetting = dto.ActivationSetting, Notes = dto.Notes });
            }
        }

        private async Task ProcessSpecificationCodesAsync(SoftwareOption option, List<SoftwareOptionSpecificationCodeCreateDto> dtos)
        {
            option.SoftwareOptionSpecificationCodes ??= new List<SoftwareOptionSpecificationCode>();
            _context.SoftwareOptionSpecificationCodes.RemoveRange(option.SoftwareOptionSpecificationCodes);
            if (dtos != null)
            {
                foreach (var dto in dtos)
                {
                    if (!option.ControlSystemId.HasValue) throw new InvalidOperationException("Cannot process specification codes for a Software Option with no Control System assigned.");

                    SpecCodeDefinition? specDef = await _context.SpecCodeDefinitions.FirstOrDefaultAsync(scd => scd.ControlSystemId == option.ControlSystemId.Value && scd.Category == dto.Category && scd.SpecCodeNo == dto.SpecCodeNo && scd.SpecCodeBit == dto.SpecCodeBit);
                    if (specDef == null)
                    {
                        specDef = new SpecCodeDefinition { ControlSystemId = option.ControlSystemId.Value, Category = dto.Category, SpecCodeNo = dto.SpecCodeNo, SpecCodeBit = dto.SpecCodeBit, Description = dto.Description };
                        _context.SpecCodeDefinitions.Add(specDef);
                    }

                    option.SoftwareOptionSpecificationCodes.Add(new SoftwareOptionSpecificationCode { SpecCodeDefinition = specDef, SoftwareOptionActivationRuleId = dto.SoftwareOptionActivationRuleId, SpecificInterpretation = dto.SpecificInterpretation });
                }
            }
        }
        #endregion
    }
}
