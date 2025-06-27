using RuleArchitect.Abstractions.DTOs.SoftwareOption;
using RuleArchitect.Abstractions.DTOs.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; // Added for Task<T>

namespace RuleArchitect.Abstractions.Interfaces
{
    public interface ISoftwareOptionService
    {
        /// <summary>
        /// Gets all software options asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains a list of software options.</returns>
        Task<List<SoftwareOptionDto>> GetAllSoftwareOptionsAsync();

        /// <summary>
        /// Gets a specific software option by its ID asynchronously.
        /// </summary>
        /// <param name="softwareOptionId">The ID of the software option.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains the software option, or null if not found.</returns>
        Task<SoftwareOptionDetailDto?> GetSoftwareOptionByIdAsync(int softwareOptionId);

        /// <summary>
        /// Creates a new software option asynchronously using the provided command DTO.
        /// </summary>
        /// <param name="command">The DTO containing the data for the new software option.</param>
        /// <param name="currentUser">The identifier for the user performing the action.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains the created software option.</returns>
        Task<SoftwareOptionDto> CreateSoftwareOptionAsync(CreateSoftwareOptionCommandDto command, int currentUserId, string currentUserName);

        /// <summary>
        /// Updates an existing software option asynchronously using the provided command DTO.
        /// </summary>
        /// <param name="command">The DTO containing the data for the software option update.</param>
        /// <param name="currentUser">The identifier for the user performing the action.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains the updated software option.</returns>
        Task<SoftwareOptionDto?> UpdateSoftwareOptionAsync(UpdateSoftwareOptionCommandDto command, int currentUserId, string currentUserName);

        /// <summary>
        /// Gets the history for a specific software option by its ID asynchronously.
        /// </summary>
        /// <param name="softwareOptionId">The ID of the software option.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains a list of software option history records.</returns>
        Task<List<SoftwareOptionHistoryDto>> GetSoftwareOptionHistoryAsync(int softwareOptionId);

        /// <summary>
        /// Deletes a software option by its ID asynchronously.
        /// </summary>
        /// <param name="softwareOptionId">The ID of the software option to delete.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the deletion was successful, false otherwise.</returns>
        Task<bool> DeleteSoftwareOptionAsync(int softwareOptionId, int currentUserId, string currentUserName); // Example delete method

        Task<List<ControlSystemLookupDto>> GetControlSystemLookupsAsync();

        Task<List<SpecCodeDefinitionDetailDto>> GetSpecCodeDefinitionsForControlSystemAsync(int controlSystemId);

        Task<SpecCodeDefinitionDetailDto?> FindSpecCodeDefinitionAsync(int controlSystemId, string category, string specCodeNo, string specCodeBit);

        Task<List<SoftwareOptionDto>> FindSoftwareOptionsByOptionNumberAsync(string optionNumber);
    }
}