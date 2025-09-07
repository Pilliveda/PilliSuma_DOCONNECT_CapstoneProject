using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoConnect.Tests.Unit
{
    /// <summary>
    /// Runs DataAnnotations validation on an object instance.
    /// </summary>
    public static class ValidationHelpers
    {
        public static IList<ValidationResult> ValidateObject(object instance)
        {
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
            return results;
        }
    }
}
