# Database Entity Service

Features completion check list

| Feature               | Description | Completion |
|:----------------------|:------------|:------------:|
| [Select](#feature-select)           | Retrieving entity from database                                 | <span style="font-size: 35px;">&#9685;</span> |
| [Compound Foreign Key](#feature-compound-foreign-key) | Foreign keys and primary keys mapping by column order | <span style="font-size: 35px;">&#9681;</span> |
| [Insert](#feature-insert)           | Insert entity into database                                     | <span style="font-size: 35px;">&#9681;</span> |
| [Update](#feature-update)           | Update entity in database                                       | <span style="font-size: 35px;">&#9681;</span> |
| [Delete](#feature-delete)           | Delete entity from database                                     | <span style="font-size: 35px;">&#9681;</span> |
| [Connection](#feature-connection)   | Resolving connection dynamically at runtime by application code | <span style="font-size: 60px;">&#9675;</span> |
| [Thread-safe](#feature-thread-save) | Test that making sure service is thread-safe.                   | <span style="font-size: 60px;">&#9675;</span> |
| [Bulk Insert](#feature-bulk-insert) | Sql server specific bulk insert                                 | <span style="font-size: 60px;">&#9675;</span> |
| [Stored Procedure](#feature-stored-procedure) | Retrieving entity from database                                 | <span style="font-size: 60px;">&#9675;</span> |

## Feature - Select

- Simple scenario tested.
- WIP in unit tests,

## Feature - Compound Foreign Key

- Design is done but ordering implementation has not finish.
- WIP in unit tests.

## Feature - Insert

- Design and implementation is done.
- WIP in unit tests.

## Feature - Update

- Design and implementation is done.
- WIP in unit tests.

## Feature - Delete

- Design and implementation is done.
- WIP in unit tests.

## Feature - Connection

- Have to think about it.
- Feature required by `XCat`

## Feature - Thread-safe

- Will need some effort to design and code test cases.
- Have not started yet.

## Feature - Bulk Insert

- Inserting data in bulk. Specific feature for Sql server using [SqlBulkCopy](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy?view=netcore-2.2).
- Have not started design yet.

## Feature - Stored Procedure

- Call database stored procedure from service.
- Have net started yet but don't expect it to be too hard.

## Appendix - Graphics - Harvey Balls

| Harvey Ball                                   | Meaning | |
|:---------------------------------------------:|---------|---|
| <span style="font-size: 60px;">&#9675;</span> | 0%      |![empty](harvey-empty.png =80x80)   |
| | 20% | ![empty](harvey_20.jpg) |
| <span style="font-size: 35px;">&#9684;</span> | 25%     |![empty](harvey-quarter.png)   |
| <span style="font-size: 35px;">&#9681;</span> | 50%     |![empty](harvey-half.png)   |
| <span style="font-size: 35px;">&#9685;</span> | 75%     |   |
| <span style="font-size: 60px;">&#9679;</span> | 100%    |![empty](harvey-full.png)   |
