# Using `ForeignKey` and `InverseProperty` attribute

_Example_

We have a parent `TCededContract` and a child `TCededContractLayer` where `TCededContract` reference `TCededContractLayer` with its property `CededContractLayers` and child `TCededContractLayer` reference the parent by its property `CededContract`, then foreign key and inverse property attribute can be applied to mark the relationship in the following ways.

## `InverseProperty` to identify the respective references on the entity object.

To link `TCededContract`.`CededContractLayers` to `TCededContractLayer`.`CededContract`, we will have

```c#
[Table("t_ceded_contract")]
public class TCededContract : ITrackable
{
	[Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ceded_contract_pkey")]
    public int CededContractPkey { get; set; }

	...
	...

	[InverseProperty(nameof(TCededContractLayer.CededContract))]
	public IList<TCededContractLayer> CededContractLayers { get; set; }	
}
```

And

```c#
public class TCededContractLayer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ceded_contract_layer_pkey")]
	public int CededContractLayerPkey { get; set; }

...
...

    [InverseProperty(nameof(TCededContract.CededContractLayers))]
    public virtual TCededContract CededContract { get; set; }
}
```

## `ForeignKey` to identify the child foreign key

There are 3 possible ways to put the foreign key attribute that can identify the relationship.

### 1. `ForeignKey` attribute on parent object

```c#

    [InverseProperty(nameof(TCededContractLayer.CededContract))]
    [ForeignKey("CededContractFkey")]
    public IList<TCededContractLayer> CededContractLayers { get; set; }
    
```

### 2. `ForeignKey` attribute on child object annotating the parent object reference.

```c#
    [Column("ceded_contract_fkey")]
    public int CededContractFkey { get; set; }
	...
	...
	[ForeignKey(nameof(CededContractFkey))]
	[InverseProperty(nameof(TCededContract.CededContractLayers))]
	public virtual TCededContract CededContract { get; set; }
```

Note that the `ForeignKey` attribute is identify the foreign key property name within the child object `TCededContractLayer`.

### 3. `ForeignKey' attribute on foreign key property.

```c#
	[Column("ceded_contract_fkey")]
	[ForeignKey(nameof(CededContract))]
	public int CededContractFkey { get; set; }
	...
	...
	[InverseProperty(nameof(TCededContract.CededContractLayers))]
	public virtual TCededContract CededContract { get; set; }
```

Note that, in the case, the `ForeignKey` attribute is identifying the parent object reference in the child object.
