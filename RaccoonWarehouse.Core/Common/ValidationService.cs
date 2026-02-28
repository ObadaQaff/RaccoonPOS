using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
namespace RaccoonWarehouse.Core.Common;
public class ValidationService
{
	public static Result<T> Validate<T>(T request, IValidator<T> validator)
	{
		var validationResult = validator.Validate(request);
		if (!validationResult.IsValid)
		{
			var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
			return Result<T>.ValidationFail(errors);
		}
		return Result<T>.Ok(request);
	}
}
