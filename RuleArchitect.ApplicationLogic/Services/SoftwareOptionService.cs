using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.Entities;
using RuleArchitect.Data; // Assuming RuleArchitectContext is in RuleArchitect.Data
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore; // Required for ToListAsync, FirstOrDefaultAsync, etc.
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.Services
{
    public class SoftwareOptionService : ISoftwareOptionService
    {
        // If you decide to inject DbContext later using Dependency Injection:
        // private readonly RuleArchitectContext _dbContext;
        // public SoftwareOptionService(RuleArchitectContext dbContext)
        // {
        // _dbContext = dbContext;
        // }

        public async Task<List<SoftwareOption>> GetAllSoftwareOptionsAsync()
        {
            using (var context = new RuleArchitectContext())
            {
                return await context.SoftwareOptions
                                    .Include(so => so.ControlSystem)
                                    .ToListAsync();
            }
        }

        public async Task<SoftwareOption?> GetSoftwareOptionByIdAsync(int softwareOptionId)
        {
            using (var context = new RuleArchitectContext())
            {
                return await context.SoftwareOptions
                                    .Include(so => so.ControlSystem)
                                    // Add other .Include() for related collections if needed immediately
                                    // .Include(so => so.OptionNumberRegistries)
                                    // .Include(so => so.Requirements)
                                    // .Include(so => so.SoftwareOptionSpecificationCodes)
                                    // .Include(so => so.SoftwareOptionActivationRules)
                                    .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId);
            }
        }

        public async Task<SoftwareOption> CreateSoftwareOptionAsync(CreateSoftwareOptionCommandDto command, string currentUser)
        {
            using (var context = new RuleArchitectContext())
            {
                // If you need an explicit transaction for all operations:
                // using (var transaction = await context.Database.BeginTransactionAsync())
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

                    context.SoftwareOptions.Add(newSoftwareOption);
                    await context.SaveChangesAsync(); // Save to get newSoftwareOption.SoftwareOptionId for FK relationships

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
                            context.OptionNumberRegistries.Add(newOnr);
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
                            context.Requirements.Add(newReq);
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
                            context.SoftwareOptionSpecificationCodes.Add(newSosc);
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
                            context.SoftwareOptionActivationRules.Add(newRule);
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
                        ControlSystemId = newSoftwareOption.ControlSystemId, // Assuming this should also be in history
                        //ChangeDescription = "Initial creation of Software Option.", // Description of the change
                        ChangeTimestamp = newSoftwareOption.LastModifiedDate,
                        ChangedBy = newSoftwareOption.LastModifiedBy
                    };
                    context.SoftwareOptionHistories.Add(initialHistory);

                    await context.SaveChangesAsync(); // Save all related entities and the history record

                    // await transaction.CommitAsync(); // Commit transaction if used
                    return newSoftwareOption;
                }
                catch (Exception ex) // Catching a general exception
                {
                    // await transaction.RollbackAsync(); // Rollback transaction if used and an error occurs
                    // Log the exception (e.g., using a logging framework like Serilog or NLog)
                    // Consider more specific exception handling if needed
                    Console.WriteLine($"An error occurred: {ex.Message}"); // Basic error logging
                    throw; // Re-throw the exception to let the caller know something went wrong
                }
                // } // Closes the transaction block if used
            } // Closes the using block for RuleArchitectContext
        }

        // You would add other methods like UpdateSoftwareOptionAsync, DeleteSoftwareOptionAsync, etc., here.
        // For example:
        // public async Task UpdateSoftwareOptionAsync(UpdateSoftwareOptionCommandDto command, string currentUser) { /* ... */ }
        // public async Task DeleteSoftwareOptionAsync(int softwareOptionId) { /* ... */ }

    } // Closes the SoftwareOptionService class
} // Closes the namespace