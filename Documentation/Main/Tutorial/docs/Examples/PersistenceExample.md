# Example of persisting entity into database.

**The service is still be tested and developed.  Thus the exact API may change but the features presented in this tutorial would stay.**

*Assumption.  For a child set reference such as IList<SomeChildObject> on a parent entity object, the service __always assume__ that the reference __is not null__.  That means it is the application responsibility to make sure that a __child list is always instantiated__ in the parent entity constructor.*

## Saving an entity into database

As before, the service would depend on the `EntityStatus` to decide the database operation required on the entity object.  The service will also handle transaction scope and isolation level.  Thus application code, for a straight forward object persistence, do not need to manage transaction.

Following example assume that we have a `CededContractLayer` as child of `CededContract` and `CededContractLayerDoc` as child of `CededContractLayer`, and we are going to insert a new `CededContractLayer` and a new `CededContractLayerDoc` into database.  Also assume that we have `_dbService` injected into the context already which can be a, say, MVC Controller.

```c#

# Retrieving the contract which has primary key 2.
var contract = _dbService.Query<TCededContract>().Where(c => c.CededContractPKey == 2).ToArray().FirstOrDefault();
# Retrieve lookup of layer type with description 'Stop Loss'
var layerType = _dbService.Query<TLookups>().Where(c => c.Description == @"Stop Loss").ToArray().FirstOrDefault();
var newLayer = new TCededContractLayer
					{
						Description = "Some layer",
						AttachmentPoint = 8000000,
						Limit = 10000000,
						LayerType = layerType, # application will set up the LayerTypeFkey on layer behind the scene.
						EntityStatus = EntityStatusEnum.New
					};

newLayer.CededContractLayerDocs.Add(
	new TCededContractLayerDoc {
		Filename = "some file.txt",
		EntityStatus = EntityStatusEnum.New
	}
);

contract.CededContractLayers.Add(newLayer);

var rowCount = _dbService
					.Persist()
					.Submit(changeSet => changeSet.Save(contract))
					.Commit();
# rowCount will be 2.
```

The key point of this example is on `_dbService.Persist().Submit(c => c.Save(contract)).Commit()` which will start a transaction, saving CededContractLayer and CededContractLayerDoc into database, and commit the transaction when `.Commit()` is called.  As 2 new rows are created in database, the return row count is 2.

Note also that the `changeSet` can `Save` multiple entity objects, or be assigned different transaction scope and/or isolation level then the default transaction that `_dbService` is setup with.  For details, please see (API reference)[http://ewrap0433d/classdocs/framework6.0/DbEntityService/html/].
