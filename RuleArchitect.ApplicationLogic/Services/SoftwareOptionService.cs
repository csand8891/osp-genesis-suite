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
            // No longer using 'using (var context = new RuleArchitectContext())'
            return await _context.SoftwareOptions
                                //.Include(so => so.ControlSystem)
                                .ToListAsync();
        }

        public async Task<SoftwareOption?> GetSoftwareOptionByIdAsync(int softwareOptionId)
        {
            return await _context.SoftwareOptions
                                .Include(so => so.ControlSystem)
                                // Add other .Include() for related collections if needed immediately
                                // .Include(so => so.OptionNumberRegistries)
                                // .Include(so => so.Requirements)
                                // .Include(so => so.SoftwareOptionSpecificationCodes)
                                // .Include(so => so.SoftwareOptionActivationRules)
                                .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId);
        }

        public async Task<SoftwareOption> CreateSoftwareOptionAsync(CreateSoftwareOptionCommandDto command, string currentUser)
        {
            // If you need an explicit transaction for all operations:
            // using (var transaction = await _context.Database.BeginTransactionAsync())
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
                            GeneralRequiredValue = reqDto.GeneralRequiredValue,
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
                    ChangeTimestamp = newSoftwareOption.LastModifiedDate,
                    ChangedBy = newSoftwareOption.LastModifiedBy
                };
                _context.SoftwareOptionHistories.Add(initialHistory);

                await _context.SaveChangesAsync(); // Save all related entities and the history record

                // await transaction.CommitAsync(); // Commit transaction if used
                return newSoftwareOption;
            }
            catch (Exception ex)
            {
                // await transaction.RollbackAsync(); // Rollback transaction if used and an error occurs
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
            // } // Closes the transaction block if used
            // The 'using' block for RuleArchitectContext is no longer here,
            // as the context's lifetime will be managed by the DI container.
        }

        // You would add other methods like UpdateSoftwareOptionAsync, DeleteSoftwareOptionAsync, etc., here.
    }
}