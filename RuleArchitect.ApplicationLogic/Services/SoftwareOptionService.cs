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
        // If you decide to inject DbContext later:
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

        public async Task<SoftwareOption> GetSoftwareOptionByIdAsync(int softwareOptionId)
        {
            using (var context = new RuleArchitectContext())
            {
                return await context.SoftwareOptions
                                    .Include(so => so.ControlSystem)
                                    // Add other .Include() for related collections if needed immediately
                                    // .Include(so => so.OptionNumberRegistries)
                                    .FirstOrDefaultAsync(so => so.SoftwareOptionId == softwareOptionId);
            }
        }

        public async Task<SoftwareOption> CreateSoftwareOptionAsync(CreateSoftwareOptionCommandDto command, string currentUser)
        {
            using (var context = new RuleArchitectContext())
            {
                // using (var transaction = await context.Database.BeginTransactionAsync()) // If explicit transaction needed
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
                        Version = 1,
                        LastModifiedDate = DateTime.UtcNow,
                        LastModifiedBy = currentUser
                    };

                    context.SoftwareOptions.Add(newSoftwareOption);
                    await context.SaveChangesAsync(); // Save to get newSoftwareOption.SoftwareOptionId

                    // 1. Option Numbers
                    if (command.OptionNumbers != null)
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
                    if (command.Requirements != null)
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
                    if (command.SpecificationCodes != null)
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
                    if (command.ActivationRules != null)
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