using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.Entities;
using RuleArchitect.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.Services
{
    /// <summary>
    /// Service for managing Software Options, including creation, retrieval, updates, and deletion.
    /// Implements ISoftwareOptionService.
    /// </summary>
    public class SoftwareOptionService : ISoftwareOptionService
    {
        private readonly RuleArchitectContext _context; // Stores the injected DbContext

        /// <summary>
        /// Initializes a new instance of the SoftwareOptionService.
        /// </summary>
        /// <param name="context">The database context, injected via DI.</param>
        public SoftwareOptionService(RuleArchitectContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); //
        }

        /// <summary>
        /// Retrieves all software options.
        /// </summary>
        /// <returns>A list of all SoftwareOption entities.</returns>
        public async Task<List<SoftwareOption>> GetAllSoftwareOptionsAsync()
        {
            return await _context.SoftwareOptions
                                .ToListAsync(); //
        }

        /// <summary>
        /// Retrieves a specific software option by its ID, including related data.
        /// </summary>
        /// <param name="softwareOptionId">The ID of the software option to retrieve.</param>
        /// <returns>The SoftwareOption entity with its related data, or null if not found.</returns>
        public async Task<SoftwareOption?> GetSoftwareOptionByIdAsync(int softwareOptionId)
        {
            return await _context.SoftwareOptions
                                .Include(so => so.ControlSystem) //
                                .Include(so => so.OptionNumberRegistries) //
                                .Include(so => so.Requirements) //
                                    .ThenInclude(r => r.RequiredSoftwareOption) //
                                .Include(so => so.Requirements) //
                                    .ThenInclude(r => r.RequiredSpecCodeDefinition) //
                                .Include(so => so.SoftwareOptionSpecificationCodes) //
                                    .ThenInclude(sosc => sosc.SpecCodeDefinition) //
                                .Include(so => so.SoftwareOptionSpecificationCodes) //
                                    .ThenInclude(sosc => sosc.SoftwareOptionActivationRule) //
                                .Include(so => so.SoftwareOptionActivationRules) //
                                .Include(so => so.ParameterMappings) //
                                .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId); //
        }

        /// <summary>
        /// Creates a new software option based on the provided command DTO.
        /// </summary>
        /// <param name="command">The DTO with data for the new option.</param>
        /// <param name="currentUser">The identifier for the user creating the option.</param>
        /// <returns>The newly created SoftwareOption entity.</returns>
        public async Task<SoftwareOption> CreateSoftwareOptionAsync(CreateSoftwareOptionCommandDto command, string currentUser)
        {
            // Basic validation (can be expanded)
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command.PrimaryName))
                throw new ArgumentException("PrimaryName is required.", nameof(command.PrimaryName));

            try
            {
                var newSoftwareOption = new SoftwareOption
                {
                    PrimaryName = command.PrimaryName,
                    AlternativeNames = command.AlternativeNames,
                    SourceFileName = command.SourceFileName,
                    PrimaryOptionNumberDisplay = command.PrimaryOptionNumberDisplay,
                    Notes = command.Notes,
                    ControlSystemId = command.ControlSystemId,
                    Version = 1, // Initial version
                    LastModifiedDate = DateTime.UtcNow,
                    LastModifiedBy = currentUser
                };

                _context.SoftwareOptions.Add(newSoftwareOption);
                // First SaveChanges to get the newSoftwareOption.SoftwareOptionId (if DB generated)
                // In tests, the Add().Callback() simulates this ID generation.
                await _context.SaveChangesAsync();

                // System.Diagnostics.Debug.WriteLine($"SoftwareOptionId after Add and SaveChanges (in service): {newSoftwareOption.SoftwareOptionId}");

                // 1. Option Numbers
                if (command.OptionNumbers != null && command.OptionNumbers.Any())
                {
                    foreach (var onrDto in command.OptionNumbers)
                    {
                        var newOnr = new OptionNumberRegistry
                        {
                            SoftwareOptionId = newSoftwareOption.SoftwareOptionId, // Use the generated/assigned ID
                            OptionNumber = onrDto.OptionNumber
                        };
                        _context.OptionNumberRegistries.Add(newOnr);
                    }
                }

                // 2. Requirements
                if (command.Requirements != null && command.Requirements.Any())
                {
                    foreach (var reqDto in command.Requirements)
                    {
                        var newReq = new Requirement
                        {
                            SoftwareOptionId = newSoftwareOption.SoftwareOptionId, // Use the generated/assigned ID
                            RequirementType = reqDto.RequirementType,
                            Condition = reqDto.Condition,
                            GeneralRequiredValue = reqDto.GeneralRequiredValue ?? string.Empty,
                            RequiredSoftwareOptionId = reqDto.RequiredSoftwareOptionId,
                            RequiredSpecCodeDefinitionId = reqDto.RequiredSpecCodeDefinitionId,
                            OspFileName = reqDto.OspFileName,
                            OspFileVersion = reqDto.OspFileVersion,
                            Notes = reqDto.Notes
                        };
                        _context.Requirements.Add(newReq);
                    }
                }

                // 3. SoftwareOptionSpecificationCodes
                if (command.SpecificationCodes != null && command.SpecificationCodes.Any())
                {
                    foreach (var soscDto in command.SpecificationCodes)
                    {
                        var newSosc = new SoftwareOptionSpecificationCode
                        {
                            SoftwareOptionId = newSoftwareOption.SoftwareOptionId, // Use the generated/assigned ID
                            SpecCodeDefinitionId = soscDto.SpecCodeDefinitionId,
                            SoftwareOptionActivationRuleId = soscDto.SoftwareOptionActivationRuleId, // Can be null
                            SpecificInterpretation = soscDto.SpecificInterpretation
                        };
                        _context.SoftwareOptionSpecificationCodes.Add(newSosc);
                    }
                }

                // 4. SoftwareOptionActivationRules
                if (command.ActivationRules != null && command.ActivationRules.Any())
                {
                    foreach (var ruleDto in command.ActivationRules)
                    {
                        var newRule = new SoftwareOptionActivationRule
                        {
                            SoftwareOptionId = newSoftwareOption.SoftwareOptionId, // Use the generated/assigned ID
                            RuleName = ruleDto.RuleName,
                            ActivationSetting = ruleDto.ActivationSetting,
                            Notes = ruleDto.Notes
                        };
                        _context.SoftwareOptionActivationRules.Add(newRule);
                    }
                }

                // Create the initial history record
                var initialHistory = new SoftwareOptionHistory
                {
                    SoftwareOptionId = newSoftwareOption.SoftwareOptionId, // Use the generated/assigned ID
                    Version = newSoftwareOption.Version, // Should be 1
                    PrimaryName = newSoftwareOption.PrimaryName,
                    AlternativeNames = newSoftwareOption.AlternativeNames,
                    SourceFileName = newSoftwareOption.SourceFileName,
                    PrimaryOptionNumberDisplay = newSoftwareOption.PrimaryOptionNumberDisplay,
                    Notes = newSoftwareOption.Notes,
                    ControlSystemId = newSoftwareOption.ControlSystemId,
                    CheckedBy = newSoftwareOption.CheckedBy, // Will be null if not set on newSoftwareOption
                    CheckedDate = newSoftwareOption.CheckedDate, // Will be null if not set on newSoftwareOption
                    ChangeTimestamp = newSoftwareOption.LastModifiedDate,
                    ChangedBy = newSoftwareOption.LastModifiedBy // Should be currentUser
                };
                _context.SoftwareOptionHistories.Add(initialHistory);

                await _context.SaveChangesAsync(); // Save all related entities and the history record

                return newSoftwareOption; // Return the option with its generated ID
            }
            catch (Exception ex)
            {
                // Log the exception (ex) properly in a real application
                Console.WriteLine($"An error occurred during CreateSoftwareOptionAsync: {ex.ToString()}");
                throw; // Re-throw to indicate failure to the caller
            }
        }

        /// <summary>
        /// Updates an existing software option based on the provided command DTO,
        /// handling partial updates for collections and ensuring versioning.
        /// </summary>
        /// <param name="command">The DTO with data for the update.</param>
        /// <param name="currentUser">The identifier for the user performing the update.</param>
        /// <returns>The updated SoftwareOption entity, or null if not found.</returns>
        public async Task<SoftwareOption?> UpdateSoftwareOptionAsync(UpdateSoftwareOptionCommandDto command, string currentUser)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync()) //
            {
                try
                {
                    var softwareOptionToUpdate = await _context.SoftwareOptions
                        .Include(so => so.OptionNumberRegistries) //
                        .Include(so => so.Requirements) //
                        .Include(so => so.SoftwareOptionSpecificationCodes) //
                        .Include(so => so.SoftwareOptionActivationRules) //
                        .FirstOrDefaultAsync(so => so.SoftwareOptionId == command.SoftwareOptionId); //

                    if (softwareOptionToUpdate == null)
                    {
                        await transaction.RollbackAsync(); //
                        return null;
                    }

                    bool anyPropertyChanged = false;

                    void UpdateProperty<T>(T currentValue, T newValue, Action<T> setter)
                    {
                        if (!EqualityComparer<T>.Default.Equals(currentValue, newValue))
                        {
                            setter(newValue);
                            anyPropertyChanged = true;
                        }
                    }

                    UpdateProperty(softwareOptionToUpdate.PrimaryName, command.PrimaryName, (v) => softwareOptionToUpdate.PrimaryName = v);
                    UpdateProperty(softwareOptionToUpdate.AlternativeNames, command.AlternativeNames, (v) => softwareOptionToUpdate.AlternativeNames = v);
                    UpdateProperty(softwareOptionToUpdate.SourceFileName, command.SourceFileName, (v) => softwareOptionToUpdate.SourceFileName = v);
                    UpdateProperty(softwareOptionToUpdate.PrimaryOptionNumberDisplay, command.PrimaryOptionNumberDisplay, (v) => softwareOptionToUpdate.PrimaryOptionNumberDisplay = v);
                    UpdateProperty(softwareOptionToUpdate.Notes, command.Notes, (v) => softwareOptionToUpdate.Notes = v);
                    UpdateProperty(softwareOptionToUpdate.ControlSystemId, command.ControlSystemId, (v) => softwareOptionToUpdate.ControlSystemId = v);
                    UpdateProperty(softwareOptionToUpdate.CheckedBy, command.CheckedBy, (v) => softwareOptionToUpdate.CheckedBy = v);
                    UpdateProperty(softwareOptionToUpdate.CheckedDate, command.CheckedDate, (v) => softwareOptionToUpdate.CheckedDate = v);

                    if (command.OptionNumbers != null)
                    {
                        _context.OptionNumberRegistries.RemoveRange(softwareOptionToUpdate.OptionNumberRegistries); //
                        softwareOptionToUpdate.OptionNumberRegistries.Clear(); //
                        foreach (var dto in command.OptionNumbers) { _context.OptionNumberRegistries.Add(new OptionNumberRegistry { SoftwareOptionId = softwareOptionToUpdate.SoftwareOptionId, OptionNumber = dto.OptionNumber }); } //
                        anyPropertyChanged = true;
                    }

                    if (command.Requirements != null)
                    {
                        _context.Requirements.RemoveRange(softwareOptionToUpdate.Requirements); //
                        softwareOptionToUpdate.Requirements.Clear(); //
                        foreach (var dto in command.Requirements) { _context.Requirements.Add(new Requirement { /* ... populate ... */ }); } //
                        anyPropertyChanged = true;
                    }

                    if (command.SpecificationCodes != null)
                    {
                        _context.SoftwareOptionSpecificationCodes.RemoveRange(softwareOptionToUpdate.SoftwareOptionSpecificationCodes); //
                        softwareOptionToUpdate.SoftwareOptionSpecificationCodes.Clear(); //
                        foreach (var dto in command.SpecificationCodes) { _context.SoftwareOptionSpecificationCodes.Add(new SoftwareOptionSpecificationCode { /* ... populate ... */ }); } //
                        anyPropertyChanged = true;
                    }

                    if (command.ActivationRules != null)
                    {
                        _context.SoftwareOptionActivationRules.RemoveRange(softwareOptionToUpdate.SoftwareOptionActivationRules); //
                        softwareOptionToUpdate.SoftwareOptionActivationRules.Clear(); //
                        foreach (var dto in command.ActivationRules) { _context.SoftwareOptionActivationRules.Add(new SoftwareOptionActivationRule { /* ... populate ... */ }); } //
                        anyPropertyChanged = true;
                    }

                    if (anyPropertyChanged)
                    {
                        softwareOptionToUpdate.Version += 1; //
                        softwareOptionToUpdate.LastModifiedDate = DateTime.UtcNow; //
                        softwareOptionToUpdate.LastModifiedBy = currentUser; //

                        _context.SoftwareOptionHistories.Add(new SoftwareOptionHistory { /* ... populate ... */ }); //
                        await _context.SaveChangesAsync(); //
                    }

                    await transaction.CommitAsync(); //
                    return softwareOptionToUpdate;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(); //
                    Console.WriteLine($"An error occurred during UpdateSoftwareOptionAsync: {ex.ToString()}"); //
                    throw; //
                }
            }
        }

        /// <summary>
        /// Retrieves the history of changes for a specific software option.
        /// </summary>
        /// <param name="softwareOptionId">The ID of the software option.</param>
        /// <returns>A list of SoftwareOptionHistory entities, ordered by version descending.</returns>
        public async Task<List<SoftwareOptionHistory>> GetSoftwareOptionHistoryAsync(int softwareOptionId)
        {
            return await _context.SoftwareOptionHistories
                                 .Where(h => h.SoftwareOptionId == softwareOptionId) //
                                 .OrderByDescending(h => h.Version) //
                                 .ToListAsync(); //
        }

        /// <summary>
        /// Deletes a software option by its ID.
        /// </summary>
        /// <param name="softwareOptionId">The ID of the software option to delete.</param>
        /// <returns>True if deletion was successful, false otherwise (e.g., not found or DB error).</returns>
        public async Task<bool> DeleteSoftwareOptionAsync(int softwareOptionId)
        {
            try
            {
                var softwareOptionToDelete = await _context.SoftwareOptions
                    .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId); //

                if (softwareOptionToDelete == null)
                {
                    return false; //
                }

                _context.SoftwareOptions.Remove(softwareOptionToDelete); //
                await _context.SaveChangesAsync(); //
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error during DeleteSoftwareOptionAsync (check FK constraints): {dbEx.ToString()}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during DeleteSoftwareOptionAsync: {ex.ToString()}"); //
                return false;
            }
        }

        public async Task<List<ControlSystemLookupDto>> GetControlSystemLookupsAsync()
        {
            return await _context.ControlSystems
                .AsNoTracking()
                .Select(cs => new ControlSystemLookupDto
                {
                    ControlSystemId = cs.ControlSystemId,
                    Name = cs.Name
                    // If you add MachineTypeName to DTO:
                    // MachineTypeName = cs.MachineType.Name // Requires .Include(cs => cs.MachineType)
                })
                .OrderBy(cs => cs.Name) // Optional: Order by name
                .ToListAsync();
        }

        public async Task<List<SpecCodeDefinitionDetailDto>> GetSpecCodeDefinitionsForControlSystemAsync(int controlSystemId)
        {
            if (controlSystemId <= 0)
            {
                return new List<SpecCodeDefinitionDetailDto>(); // Or throw ArgumentOutOfRangeException
            }

            return await _context.SpecCodeDefinitions
                .AsNoTracking()
                .Include(scd => scd.ControlSystem) // Include ControlSystem to get its name
                .Where(scd => scd.ControlSystemId == controlSystemId)
                .Select(scd => new SpecCodeDefinitionDetailDto
                {
                    SpecCodeDefinitionId = scd.SpecCodeDefinitionId,
                    ControlSystemId = scd.ControlSystemId,
                    ControlSystemName = scd.ControlSystem.Name, // Assumes ControlSystem entity is loaded
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
                // Or throw ArgumentException for invalid parameters
                return null;
            }

            var specCodeDefinition = await _context.SpecCodeDefinitions
                .AsNoTracking()
                .Include(scd => scd.ControlSystem) // Include ControlSystem to get its name
                .FirstOrDefaultAsync(scd =>
                    scd.ControlSystemId == controlSystemId &&
                    scd.Category == category &&
                    scd.SpecCodeNo == specCodeNo &&
                    scd.SpecCodeBit == specCodeBit);

            if (specCodeDefinition == null)
            {
                return null;
            }

            return new SpecCodeDefinitionDetailDto
            {
                SpecCodeDefinitionId = specCodeDefinition.SpecCodeDefinitionId,
                ControlSystemId = specCodeDefinition.ControlSystemId,
                ControlSystemName = specCodeDefinition.ControlSystem.Name, // Assumes ControlSystem entity is loaded
                Category = specCodeDefinition.Category,
                SpecCodeNo = specCodeDefinition.SpecCodeNo,
                SpecCodeBit = specCodeDefinition.SpecCodeBit,
                Description = specCodeDefinition.Description
            };
        }
    }
}