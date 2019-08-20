# Test Sample Setup

## Database and data setup

All scripts can be found under Scripts folder of Test.Sample project.

| Filename            | Purpose                                                    |
|---------------------|------------------------------------------------------------|
| Create_ceded_DB.sql | Create DB, tables and stored proc, and insert sample data. |
| DeleteDB.sql        | Remove the database itself.                                |
| Test_DB.sql         | Script to test Create_ceded_DB.sql                         |

## Test.Sample Project

1. Setup. This project uses direct project reference to get AXAXL.DbEntity library.  __May want to switch to nuget package in the future when code is more stable.__
1. Test Run.  Currently the connection to database is hardcoded in the following 2 files.  **(2019-08-20 PSham note: So far I'm utilizing a local SQL database for testing)**
    - Program.cs under project root folder; and
	- HelperMethods.cs under Models folder.
