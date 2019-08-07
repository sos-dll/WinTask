# WinTask
A really simple `Task` wrapper for the [TaskScheduler library](https://github.com/dahall/TaskScheduler), made for better code readability.

# Examples
## Create a daily task, testing for equality, and removing a task
```cs
const string TaskName = "MyDailyTask"; // Task name (keep it unique).
string exePath = new FileInfo(Assembly.GetExecutingAssembly().Location).FullName; // A full path of this program, which will be executed on daily basis.
using (var task = WinTask.With(TaskName)                 // Get existing -or- create a new task.
                         .Description("The daily task")  // Set a new description for this task.
                         .RemoveTriggers()               // Removes all triggers (on existing task).
                         .DailyTrigger()                 // Add a new daily trigger.
                         .RemoveActions()                // Removes all actions (on existing task).
                         .ExecAction(exePath)            // Add an action.
                         .Update())                      // All ready, update the task definition.
{ /* do something more with the task in here, if needed */
    Task t = task;            // Implicit cast from WinTask to Task.
    var winTask = (WinTask)t; // Explicit cast from Task to WinTask.
    bool areEqual = winTask == task; // Implemented equality test, yay.
    if (areEqual) {
        Console.WriteLine("These two are the same task.");
    } else {
        Console.Error.WriteLine("Something has gone wrong with equality test! Please report a bug.");
    }
    task.Delete(); // Delete the task.
}
```

## Create a (special) monthly task
For example, if you wanted to create a task which is run on 26th February, and on 30th for every other month:
```cs
const string MonthlyTask = "MyMonthlyTask";
using (var task = WinTask.With(MonthlyTask)
                         .Description("The monthly task")
                         .RemoveTriggers()
                         .MonthlyTrigger(26, MonthsOfTheYear.February)
                         .MonthlyTrigger(30, MonthsOfTheYear.AllMonths & ~MonthsOfTheYear.February)
                         .RemoveActions()
                         .ExecAction(exePath)
                         .Update())
{ /* do something more with the task in here, if needed */
}
```

## Omitted-using syntax:
You can also work with `WinTask` without `using`-statement:
```cs
WinTask.With("YourTaskName")
       /* stuff */
       .Update()
       .Dispose(); // Make sure you do call Dispose at the end!
```

## Test if there is a task with the given name
```cs
bool taskExists = WinTask.Get("YourTaskName"); // Implicit bool operator for WinTask allows you to do this :)
```

## Quickly remove a task by name
```cs
WinTask.Get("YourTaskName")?.Delete(true);
```

# Downloads
You can download the latest build from the [releases page](https://github.com/sos-dll/WinTask/releases/latest).

# License
`WinTask` uses the same license as its core library, see [TaskScheduler](https://github.com/dahall/TaskScheduler/blob/master/license.md). As of this post date (7th August 2019), it is licensed under MIT.
