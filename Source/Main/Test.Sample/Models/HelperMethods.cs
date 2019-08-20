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
			var connectionString = @"Server=gp01.cyberia0083.org,1433; Database=ORMPOCDB; User Id=sa; Password=tit@ns8in";

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
		// TODO: Need to figure this out later.
		public static string CurrentUserId => "Testing";
	}
}
