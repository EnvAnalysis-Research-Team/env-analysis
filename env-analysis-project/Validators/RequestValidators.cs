using System;
using System.Collections.Generic;
using env_analysis_project.Controllers;
using env_analysis_project.Models;

namespace env_analysis_project.Validators
{
    public static class MeasurementResultValidator
    {
        public static IReadOnlyCollection<string> Validate(MeasurementResultsController.MeasurementResultRequest? request)
        {
            var errors = new List<string>();
            if (request == null)
            {
                errors.Add("Measurement result payload is required.");
                return errors;
            }

            if (request.EmissionSourceId <= 0)
            {
                errors.Add("Emission source is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ParameterCode))
            {
                errors.Add("Parameter code is required.");
            }

            if (request.MeasurementDate == default)
            {
                errors.Add("Measurement date is required.");
            }

            if (request.Value is < 0)
            {
                errors.Add("Value cannot be negative.");
            }

            if (request.IsApproved && request.ApprovedAt is null)
            {
                errors.Add("Approved results must include an ApprovedAt value.");
            }

            if (!string.IsNullOrWhiteSpace(request.Unit) && request.Unit!.Length > 50)
            {
                errors.Add("Unit cannot exceed 50 characters.");
            }

            if (!string.IsNullOrWhiteSpace(request.Remark) && request.Remark!.Length > 500)
            {
                errors.Add("Remark cannot exceed 500 characters.");
            }

            return errors;
        }
    }

    public static class EmissionSourceValidator
    {
        public static IReadOnlyCollection<string> Validate(EmissionSource? model)
        {
            var errors = new List<string>();
            if (model == null)
            {
                errors.Add("Emission source payload is required.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(model.SourceCode))
            {
                errors.Add("Source code is required.");
            }

            if (string.IsNullOrWhiteSpace(model.SourceName))
            {
                errors.Add("Source name is required.");
            }

            if (model.SourceTypeID <= 0)
            {
                errors.Add("A valid source type is required.");
            }

            if (model.Latitude is < -90 or > 90)
            {
                errors.Add("Latitude must be between -90 and 90.");
            }

            if (model.Longitude is < -180 or > 180)
            {
                errors.Add("Longitude must be between -180 and 180.");
            }

            return errors;
        }

        public static IReadOnlyCollection<string> ValidateDelete(EmissionSourcesController.DeleteEmissionSourceRequest? request)
        {
            var errors = new List<string>();
            if (request == null)
            {
                errors.Add("Delete request payload is required.");
                return errors;
            }

            if (request.Id <= 0)
            {
                errors.Add("Invalid emission source identifier.");
            }

            return errors;
        }
    }

    public static class SourceTypeValidator
    {
        public static IReadOnlyCollection<string> Validate(SourceType? model)
        {
            var errors = new List<string>();
            if (model == null)
            {
                errors.Add("Source type payload is required.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(model.SourceTypeName))
            {
                errors.Add("Source type name is required.");
            }
            else if (model.SourceTypeName.Length > 200)
            {
                errors.Add("Source type name cannot exceed 200 characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.Description) && model.Description.Length > 1000)
            {
                errors.Add("Description cannot exceed 1000 characters.");
            }

            return errors;
        }
    }

    public static class ParameterValidator
    {
        public static IReadOnlyCollection<string> Validate(Parameter? parameter)
        {
            var errors = new List<string>();
            if (parameter == null)
            {
                errors.Add("Parameter payload is required.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(parameter.ParameterCode))
            {
                errors.Add("Parameter code is required.");
            }

            if (string.IsNullOrWhiteSpace(parameter.ParameterName))
            {
                errors.Add("Parameter name is required.");
            }

            if (parameter.StandardValue is < 0)
            {
                errors.Add("Standard value cannot be negative.");
            }

            if (!string.IsNullOrWhiteSpace(parameter.Unit) && parameter.Unit.Length > 50)
            {
                errors.Add("Unit cannot exceed 50 characters.");
            }

            if (!string.IsNullOrWhiteSpace(parameter.Description) && parameter.Description.Length > 1000)
            {
                errors.Add("Description cannot exceed 1000 characters.");
            }

            if (!ParameterTypeHelper.IsValid(parameter.Type))
            {
                errors.Add("Parameter type must be either 'water' or 'air'.");
            }

            return errors;
        }

        public static IReadOnlyCollection<string> ValidateDto(ParametersController.ParameterDto? dto, bool isUpdate = false)
        {
            var errors = new List<string>();
            if (dto == null)
            {
                errors.Add("Parameter payload is required.");
                return errors;
            }

            if (!isUpdate && string.IsNullOrWhiteSpace(dto.ParameterCode))
            {
                errors.Add("Parameter code is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.ParameterName))
            {
                errors.Add("Parameter name is required.");
            }

            if (dto.StandardValue is < 0)
            {
                errors.Add("Standard value cannot be negative.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Unit) && dto.Unit.Length > 50)
            {
                errors.Add("Unit cannot exceed 50 characters.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > 1000)
            {
                errors.Add("Description cannot exceed 1000 characters.");
            }

            if (!ParameterTypeHelper.IsValid(dto.Type))
            {
                errors.Add("Parameter type must be either 'water' or 'air'.");
            }

            return errors;
        }

        public static IReadOnlyCollection<string> ValidateIdentifier(string? code)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(code))
            {
                errors.Add("Parameter code is required.");
            }
            return errors;
        }
    }

    public static class UserValidator
    {
        public static IReadOnlyCollection<string> Validate(ApplicationUser? user, bool requireId = false)
        {
            var errors = new List<string>();
            if (user == null)
            {
                errors.Add("User payload is required.");
                return errors;
            }

            if (requireId && string.IsNullOrWhiteSpace(user.Id))
            {
                errors.Add("User identifier is required.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                errors.Add("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                errors.Add("Full name is required.");
            }

            if (!string.IsNullOrWhiteSpace(user.Role) && user.Role.Length > 100)
            {
                errors.Add("Role cannot exceed 100 characters.");
            }

            return errors;
        }

        public static IReadOnlyCollection<string> ValidateForUpdate(ApplicationUser? user) =>
            Validate(user, requireId: true);
    }
}
