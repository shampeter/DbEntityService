using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace Test.Sample.Models
{
	public class HelperMethods
	{
		public static int NextSequence(int argType, int argRange)
		{
			int result;
			var connectionString = @"Server=localhost,1433; Database=DbEntityServiceTestDb; User Id=DbEntityService; Password=Password1";

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var cmd = new SqlCommand("[dbo].[spu_getguid]", connection);
				cmd.CommandType = CommandType.StoredProcedure;
				var type = new SqlParameter("@seq_type", SqlDbType.Int);
				type.Value = argType;
				type.Direction = ParameterDirection.Input;
				var range = new SqlParameter("@range", SqlDbType.Int);
				range.Value = argRange;
				range.Direction = ParameterDirection.Input;
				var nextSeq = new SqlParameter("@next_seq", SqlDbType.Int);
				nextSeq.Direction = ParameterDirection.Output;
				cmd.Parameters.Add(type);
				cmd.Parameters.Add(range);
				cmd.Parameters.Add(nextSeq);
				cmd.Prepare();
				cmd.ExecuteNonQuery();
				result = Convert.ToInt32(cmd.Parameters["@next_seq"].Value);
			}
			return result;
		}
		// TODO: Need to figure out how to get the session user Id.  This is left for security service design where we may use JWT to carry user's claims round a user sessin.
		public static string CurrentUserId => "Testing";
	}
}
