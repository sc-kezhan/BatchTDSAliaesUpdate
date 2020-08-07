# BatchTDSAliaesUpdate
Batch update all items and project file in a TDS Visual Studio project to use letter aliaes.   
Supports item seralization only, no YAML support. Does not support runing multiple times with a project.

## Usage
Checkout repo and build the solution, then run below in cmd(admin)
```
BatchTDSAliaesUpdate.exe "Path to xxx.TDS.master.csproj"
```

## Note
Half baked POC code, please use with caution, backup the project file and all items before every use.   
V2 plan(may never happen): use TDS library to make the logic more robust.
