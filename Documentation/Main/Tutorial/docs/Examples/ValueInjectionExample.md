# Value Injection Example

Since the service does not put any requirement on the entity object design, if application still wants to update column, say, `modify_dt` as Framework 5.0 did, here is how.

```c#
[ValueInjection(FunctionScript = "() => DateTime.Now", When = InjectionOptions.WhenInsertedAndUpdated)]
[Column("modify_dt")]
public DateTime ModifyDate { get; set; }
```

The string specified in `FunctionScript` attribute will be compiled into a `Func<dynamic>` which will be used by the service to assign whatever value required, in this case the current datetime, to the annotated property before insert or update or both.  In this example, the value is assigned to `ModifyDate` before database insert and update.

For the same token, if application wants the service to update an entity object before database operation, say `Version += 1` as in Framework 5.0 concurrency update, one can do this instead.

```c#
[Column("version")]
[ConcurrencyCheck]
[ActionInjection(ActionScript = "(a) => ((TCompany)a).Version += 1", When = InjectionOptions.WhenUpdated)]
public int Version { get; set; }
```

The `ActionScript` will be compiled as `Action<object>` which will be called upon before database operation.

## Notes

For the `added_by`, `modify_by`, `added_app` and  `modify_app`, there is no definite design decision in updating them in Framework 6.0 yet because the team is still researching to accomplish such functionalities in a more industrial standard way.  Nevertheless, with the injection mechanism in place, we can delay such design decision until the related research is done.
