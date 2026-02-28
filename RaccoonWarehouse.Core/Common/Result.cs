using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Core.Common
{
	public class Result<T>
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public T? Data { get; set; }
		public List<string> Errors { get; set; }

		private Result(bool success, T? data, string message, List<string> errors)
		{
			Success = success;
			Data = data;
			Message = message;
			Errors = errors ?? new List<string>();
		}

		public static Result<T> Ok(T? data, string message = "Success")
			=> new Result<T>(true, data, message, null);

		public static Result<T?> Fail(string message, List<string> errors = null)
			=> new Result<T?>(false, default, message, errors ?? new List<string>());
	
		public static Result<T> ValidationFail(List<string> errors)
			=> new Result<T>(false, default, "Validation failed", errors);
	
	
	
	}

    public class Result
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }

        private Result(bool success, string message, List<string> errors)
        {
            Success = success;
            Message = message;
            Errors = errors ?? new List<string>();
        }

        public static Result Ok(string message = "Success")
            => new Result(true, message, null);

        public static Result Fail(string message, List<string>? errors = null)
            => new Result(false, message, errors ?? new List<string>());
    }

}
