using System;
using System.Linq;

namespace Microsoft.Win32.TaskScheduler
{
    public sealed class WinTask : IDisposable, IEquatable<WinTask>
    {
        private TaskService _service;
        private TaskDefinition _taskDefinition;

        public WinTask(string name, bool createIfNotFound = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _service = new TaskService();
            using (var rootFolder = _service.RootFolder)
            {
                Task = rootFolder.EnumerateTasks(t => String.Equals(t.Name, name, StringComparison.Ordinal)).FirstOrDefault();
                if (Task == null)
                {
                    if (createIfNotFound)
                    {
                        _taskDefinition = _service.NewTask();
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    _taskDefinition = Task.Definition;
                }
            }
        }

        public WinTask(in Task task)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            _service = Task.TaskService;
            _taskDefinition = Task.Definition;
        }

        public string Name
        {
            get;
        }

        public Task Task
        {
            get;
            private set;
        }

        private bool Disposed
        {
            get;
            set;
        }

        public static WinTask Get(in string name)
        {
            try
            {
                return new WinTask(name, false);
            }
            catch
            {
                return null;
            }
        }

        public static WinTask With(in string name) => new WinTask(name, true);

        public WinTask Description(string description)
        {
            _taskDefinition.RegistrationInfo.Description = description;
            return this;
        }

        public WinTask RemoveActions()
        {
            _taskDefinition.Actions.Clear();
            return this;
        }

        public WinTask Action(Action action)
        {
            _taskDefinition.Actions.Add(action);
            return this;
        }

        public WinTask Actions(params Action[] actions)
        {
            _taskDefinition.Actions.AddRange(actions);
            return this;
        }

        public WinTask ExecAction(string path, string arguments = null, string workingDirectory = null)
        {
            _taskDefinition.Actions.Add(new ExecAction(path, arguments, workingDirectory));
            return this;
        }

        public WinTask RemoveTriggers()
        {
            _taskDefinition.Triggers.Clear();
            return this;
        }

        public WinTask Trigger(Trigger trigger)
        {
            _taskDefinition.Triggers.Add(trigger);
            return this;
        }

        public WinTask Triggers(params Trigger[] triggers)
        {
            _taskDefinition.Triggers.AddRange(triggers);
            return this;
        }

        public WinTask TimeTrigger(DateTime startBoundary)
        {
            _taskDefinition.Triggers.Add(new TimeTrigger(startBoundary));
            return this;
        }

        public WinTask DailyTrigger(short daysInterval = 1)
        {
            _taskDefinition.Triggers.Add(new DailyTrigger(daysInterval));
            return this;
        }

        public WinTask MonthlyTrigger(int dayOfMonth = 1, MonthsOfTheYear monthsOfYear = MonthsOfTheYear.AllMonths)
        {
            _taskDefinition.Triggers.Add(new MonthlyTrigger(dayOfMonth, monthsOfYear));
            return this;
        }

        public WinTask MonthlyTrigger(int[] daysOfMonth = null, MonthsOfTheYear monthsOfYear = MonthsOfTheYear.AllMonths)
        {
            if (daysOfMonth == null)
            {
                daysOfMonth = new[] { 1 };
            }
            _taskDefinition.Triggers.Add(new MonthlyTrigger { DaysOfMonth = daysOfMonth, MonthsOfYear = monthsOfYear });
            return this;
        }

        public WinTask Update()
        {
            _taskDefinition.Validate(true);
            using (var rootFolder = _service.RootFolder)
            {
                Task = rootFolder.RegisterTaskDefinition(Name, _taskDefinition);
            }
            return this;
        }

        public bool Run(params string[] parameters)
        {
            try
            {
                var runningTask = Task.Run(parameters);
                var state = runningTask.State;
                return state == TaskState.Running || state == TaskState.Queued;
            }
            catch
            {
                return false;
            }
        }

        public bool Stop()
        {
            try
            {
                Task.Stop();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool Delete(bool dispose = false)
        {
            try
            {
                Stop();
                using (var rootFolder = _service.RootFolder)
                {
                    rootFolder.DeleteTask(Name, true);
                }
            }
            catch
            {
                return false;
            }
            if (dispose)
            {
                Dispose();
            }
            return true;
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            if (_taskDefinition != null)
            {
                _taskDefinition.Dispose();
                _taskDefinition = null;
            }
            if (Task != null)
            {
                Task.Dispose();
                Task = null;
            }
            if (_service != null)
            {
                _service.Dispose();
                _service = null;
            }
            Disposed = true;
        }

        public bool Equals(WinTask other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            //String.Equals(_taskDefinition.XmlText, other._taskDefinition.XmlText, StringComparison.Ordinal); // BUG: StartBoundary on the other instance contains milliseconds for some reason, the two which are indeed the same, turns out to be unequal.
            if (Task == null || other.Task == null)
            {
                return false;
            }
            return Task.Path == other.Task.Path;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is WinTask other && Equals(other);

        public override int GetHashCode() => _taskDefinition != null ? _taskDefinition.XmlText.GetHashCode() : 0;

        public static bool operator ==(WinTask left, WinTask right) => Equals(left, right);

        public static bool operator !=(WinTask left, WinTask right) => !(left == right);

        public static bool operator !(WinTask winTask) => winTask == null || winTask.Disposed;

        public static explicit operator WinTask(Task task) => new WinTask(in task);

        public static implicit operator Task(WinTask winTask) => winTask.Task;

        public static implicit operator bool(WinTask winTask) => !!winTask;
    }
}
