# Unit Test Database and Data Creation and Maintenance

## Importing DacPac into SqlLoalDb for Update

If, in any case, the unit test seed data needed to be updated, using the
`SQL Server Object Explorer` from `SQL Server Data Tools` or `SSDT`, open a
connection to your `(LocalDb)\MSSqlLocalDB`, right click on the `Databases` folder
and then choose `Publish Data-tier Application...`. Following the dialog, choose the
`DacPac` file under the Seed folder, click `Publish`.

There will be a warning that say any existing data in the tables will be removed because the `DacPac` contains data.
Just choose `Ok` to continue.  When finished, the target database will be created with seed data.

Use `SSDT` to add data and design tables or stored procedure.

## Extract DacPac from Existing Database with Data.

In `SQL Server Object Explorer` of `SSDT`, choose the database you want to export, right click
and choose `Extract Data-tier application...`.  Specify a filename and be reminded to
choose `Extract schema and data`.  When done, the exported `DacPac` file will
replace the file under `Seed` folder for future unit testing. 

## NuGet Package Required to deploy DacPac during Unit Tests

> At the time of this writing, only this preview version 150.4519.1 works.  All
> earlier version worked on .NET Framework only, not .NET Core project.

Install the preview version by, say, Package Manager with
```nuget
PM> Install-Package Microsoft.SqlServer.DACFx -Version 150.4519.1-preview
```
