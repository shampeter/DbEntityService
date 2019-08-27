# Solutoin Structure

1. Documentation

    - Folder for document concerning this solution such as this README.md.
    - Documentation of this service is on another branch on the repo and is not connected to this solution.

2. SourceCode

    - Source code of this service.  All code goes to one project organized by folders.

3. UnitTests

    - Unit test projects go here.
    - Unit tests utilize `SqlLocalDb`.
    - Unit tests utilize a DacPac named `DbEntityServiceUnitTestDb.dacpac` located under AXAXL.DbEntity.UnitTestLib/TestData/Seed as seed data for tests.
    - Refer to [this README.md](./AXAXL.DbEntity.UnitTestLib/TestData/README.md) on how to update and maintain this seed data.
