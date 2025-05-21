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
    public class SoftwareOptionService : ISoftwareOptionService
    {
        private readonly RuleArchitectContext _context; // Store the injected context

        // Constructor to accept the DbContext via Dependency Injection
        public SoftwareOptionService(RuleArchitectContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<SoftwareOption>> GetAllSoftwareOptionsAsync()
        {
            return await _context.SoftwareOptions
                                //.Include(so => so.ControlSystem) // Example: Eager load if needed for list views
                                .ToListAsync();
        }

        public async Task<SoftwareOption?> GetSoftwareOptionByIdAsync(int softwareOptionId)
        {
            return await _context.SoftwareOptions
                                .Include(so => so.ControlSystem)
                                .Include(so => so.OptionNumberRegistries)
                                .Include(so => so.Requirements)
                                    .ThenInclude(r => r.RequiredSoftwareOption)
                                .Include(so => so.Requirements)
                                    .ThenInclude(r => r.RequiredSpecCodeDefinition)
                                .Include(so => so.SoftwareOptionSpecificationCodes)
                                    .ThenInclude(sosc => sosc.SpecCodeDefinition)
                                .Include(so => so.SoftwareOptionSpecificationCodes)
                                    .ThenInclude(sosc => sosc.SoftwareOptionActivationRule)
                                .Include(so => so.SoftwareOptionActivationRules)
                                .Include(so => so.ParameterMappings) // Added based on SoftwareOption entity
                                .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId);
        }

        public async Task<SoftwareOption> CreateSoftwareOptionAsync(CreateSoftwareOptionCommandDto command, string currentUser)
        {
            // using (var transaction = await _context.Database.BeginTransactionAsync()) // Optional: if needed for extended operations not shown
            // {
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
                    // CheckedBy and CheckedDate are not in CreateSoftwareOptionCommandDto, will be null/default
                };

                _context.SoftwareOptions.Add(newSoftwareOption);
                await _context.SaveChangesAsync(); // Save to get newSoftwareOption.SoftwareOptionId for FK relationships

                // 1. Option Numbers
                if (command.OptionNumbers != null && command.OptionNumbers.Any())
                {
                    foreach (var onrDto in command.OptionNumbers)
                    {
                        var newOnr = new OptionNumberRegistry
                        {
                            SoftwareOptionId = newSoftwareOption.SoftwareOptionId,
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
                            SoftwareOptionId = newSoftwareOption.SoftwareOptionId,
                            RequirementType = reqDto.RequirementType,
                            Condition = reqDto.Condition,
                            GeneralRequiredValue = reqDto.GeneralRequiredValue ?? string.Empty, // Ensure non-null if DB column is non-null
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
                            SoftwareOptionId = newSoftwareOption.SoftwareOptionId,
                            SpecCodeDefinitionId = soscDto.SpecCodeDefinitionId,
                            SoftwareOptionActivationRuleId = soscDto.SoftwareOptionActivationRuleId,
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
                            SoftwareOptionId = newSoftwareOption.SoftwareOptionId,
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
                    SoftwareOptionId = newSoftwareOption.SoftwareOptionId,
                    Version = newSoftwareOption.Version,
                    PrimaryName = newSoftwareOption.PrimaryName,
                    AlternativeNames = newSoftwareOption.AlternativeNames,
                    SourceFileName = newSoftwareOption.SourceFileName,
                    PrimaryOptionNumberDisplay = newSoftwareOption.PrimaryOptionNumberDisplay,
                    Notes = newSoftwareOption.Notes,
                    ControlSystemId = newSoftwareOption.ControlSystemId,
                    CheckedBy = newSoftwareOption.CheckedBy,
                    CheckedDate = newSoftwareOption.CheckedDate,
                    ChangeTimestamp = newSoftwareOption.LastModifiedDate,
                    ChangedBy = newSoftwareOption.LastModifiedBy
                };
                _context.SoftwareOptionHistories.Add(initialHistory);

                await _context.SaveChangesAsync(); // Save all related entities and the history record

                // await transaction.CommitAsync();
                return newSoftwareOption;
            }
            catch (Exception ex)
            {
                // await transaction.RollbackAsync();
                Console.WriteLine($"An error occurred during CreateSoftwareOptionAsync: {ex.ToString()}");
                throw;
            }
            // }
        }

        public async Task<SoftwareOption?> UpdateSoftwareOptionAsync(UpdateSoftwareOptionCommandDto command, string currentUser)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var softwareOptionToUpdate = await _context.SoftwareOptions
                        .Include(so => so.OptionNumberRegistries)
                        .Include(so => so.Requirements)
                        .Include(so => so.SoftwareOptionSpecificationCodes)
                        .Include(so => so.SoftwareOptionActivationRules)
                        .FirstOrDefaultAsync(so => so.SoftwareOptionId == command.SoftwareOptionId);

                    if (softwareOptionToUpdate == null)
                    {
                        await transaction.RollbackAsync(); // Rollback if entity not found
                        return null;
                    }

                    // Update scalar properties
                    softwareOptionToUpdate.PrimaryName = command.PrimaryName;
                    softwareOptionToUpdate.AlternativeNames = command.AlternativeNames;
                    softwareOptionToUpdate.SourceFileName = command.SourceFileName;
                    softwareOptionToUpdate.PrimaryOptionNumberDisplay = command.PrimaryOptionNumberDisplay;
                    softwareOptionToUpdate.Notes = command.Notes;
                    softwareOptionToUpdate.ControlSystemId = command.ControlSystemId;
                    // Assuming CheckedBy/Date are not updated via this DTO, or add them to DTO
                    // softwareOptionToUpdate.CheckedBy = command.CheckedBy;
                    // softwareOptionToUpdate.CheckedDate = command.CheckedDate;

                    softwareOptionToUpdate.Version += 1;
                    softwareOptionToUpdate.LastModifiedDate = DateTime.UtcNow;
                    softwareOptionToUpdate.LastModifiedBy = currentUser;

                    // 1. Update OptionNumberRegistries
                    _context.OptionNumberRegistries.RemoveRange(softwareOptionToUpdate.OptionNumberRegistries.ToList());
                    softwareOptionToUpdate.OptionNumberRegistries.Clear();
                    if (command.OptionNumbers != null)
                    {
                        foreach (var onrDto in command.OptionNumbers)
                        {
                            var newOnr = new OptionNumberRegistry
                            {
                                SoftwareOptionId = softwareOptionToUpdate.SoftwareOptionId,
                                OptionNumber = onrDto.OptionNumber
                            };
                            _context.OptionNumberRegistries.Add(newOnr);
                        }
                    }

                    // 2. Update Requirements
                    _context.Requirements.RemoveRange(softwareOptionToUpdate.Requirements.ToList());
                    softwareOptionToUpdate.Requirements.Clear();
                    if (command.Requirements != null)
                    {
                        foreach (var reqDto in command.Requirements)
                        {
                            var newReq = new Requirement
                            {
                                SoftwareOptionId = softwareOptionToUpdate.SoftwareOptionId,
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

                    // 3. Update SoftwareOptionSpecificationCodes
                    _context.SoftwareOptionSpecificationCodes.RemoveRange(softwareOptionToUpdate.SoftwareOptionSpecificationCodes.ToList());
                    softwareOptionToUpdate.SoftwareOptionSpecificationCodes.Clear();
                    if (command.SpecificationCodes != null)
                    {
                        foreach (var soscDto in command.SpecificationCodes)
                        {
                            var newSosc = new SoftwareOptionSpecificationCode
                            {
                                SoftwareOptionId = softwareOptionToUpdate.SoftwareOptionId,
                                SpecCodeDefinitionId = soscDto.SpecCodeDefinitionId,
                                SoftwareOptionActivationRuleId = soscDto.SoftwareOptionActivationRuleId,
                                SpecificInterpretation = soscDto.SpecificInterpretation
                            };
                            _context.SoftwareOptionSpecificationCodes.Add(newSosc);
                        }
                    }

                    // 4. Update SoftwareOptionActivationRules
                    _context.SoftwareOptionActivationRules.RemoveRange(softwareOptionToUpdate.SoftwareOptionActivationRules.ToList());
                    softwareOptionToUpdate.SoftwareOptionActivationRules.Clear();
                    if (command.ActivationRules != null)
                    {
                        foreach (var ruleDto in command.ActivationRules)
                        {
                            var newRule = new SoftwareOptionActivationRule
                            {
                                SoftwareOptionId = softwareOptionToUpdate.SoftwareOptionId,
                                RuleName = ruleDto.RuleName,
                                ActivationSetting = ruleDto.ActivationSetting,
                                Notes = ruleDto.Notes
                            };
                            _context.SoftwareOptionActivationRules.Add(newRule);
                        }
                    }

                    // Create a new history record for the update
                    var historyRecord = new SoftwareOptionHistory
                    {
                        SoftwareOptionId = softwareOptionToUpdate.SoftwareOptionId,
                        Version = softwareOptionToUpdate.Version,
                        PrimaryName = softwareOptionToUpdate.PrimaryName,
                        AlternativeNames = softwareOptionToUpdate.AlternativeNames,
                        SourceFileName = softwareOptionToUpdate.SourceFileName,
                        PrimaryOptionNumberDisplay = softwareOptionToUpdate.PrimaryOptionNumberDisplay,
                        Notes = softwareOptionToUpdate.Notes,
                        ControlSystemId = softwareOptionToUpdate.ControlSystemId,
                        CheckedBy = softwareOptionToUpdate.CheckedBy,
                        CheckedDate = softwareOptionToUpdate.CheckedDate,
                        ChangeTimestamp = softwareOptionToUpdate.LastModifiedDate,
                        ChangedBy = softwareOptionToUpdate.LastModifiedBy
                    };
                    _context.SoftwareOptionHistories.Add(historyRecord);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return softwareOptionToUpdate;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"An error occurred during UpdateSoftwareOptionAsync: {ex.ToString()}");
                    throw; // Re-throw the exception to indicate failure
                }
            }
        }

        public async Task<List<SoftwareOptionHistory>> GetSoftwareOptionHistoryAsync(int softwareOptionId)
        {
            return await _context.SoftwareOptionHistories
                                 .Where(h => h.SoftwareOptionId == softwareOptionId)
                                 .OrderByDescending(h => h.Version)
                                 .ToListAsync();
        }

        public async Task<bool> DeleteSoftwareOptionAsync(int softwareOptionId)
        {
            try
            {
                var softwareOptionToDelete = await _context.SoftwareOptions
                    .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId);

                if (softwareOptionToDelete == null)
                {
                    return false;
                }

                _context.SoftwareOptions.Remove(softwareOptionToDelete);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during DeleteSoftwareOptionAsync: {ex.ToString()}");
                return false; // Or re-throw depending on desired error handling
            }
        }
    }
}