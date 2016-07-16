# "ZombieProcesses" Test
Simple test to check when zombie processes become reaped and when they live until primary process ends.

This project is written in C# language and can be compiled with Mono 3.x/4.x to work on Linux platforms.

### Theory: What is zombie process?
This is just dead process, but not real process. There are no other resources associated with the zombie process: it doesn't have any memory or any running code, it doesn't hold any files open, etc. Event it doesn't have real ID in process table, but it's entry still appear in process table. Why? When process finishes execution, it'll have an exit status to be reported to it's parent process. This process entry is kept around, forming a zombie, to allow the parent process to track the exit status of the child. 

The parent reads the exit status by calling one of the [wait](http://pubs.opengroup.org/onlinepubs/009695399/functions/wait.html) family of syscalls; at this point, the zombie disappears. Calling wait function(-s) is said to reap the child, extending the metaphor of a zombie being dead but in some way still not fully processed into the afterlife. The parent can also indicate that it doesn't care (by ignoring the SIGCHLD signal, or by calling [sigaction](http://pubs.opengroup.org/onlinepubs/009695399/functions/sigaction.html) with the SA_NOCLDWAIT flag), in which case the entry in the process table is deleted immediately when the child dies.

Thus a zombie only exists when a process has died and its parent hasn't called wait yet. This state can only last as long as the parent is still running. If the parent dies before the child or dies without reading the child's status, the zombie's parent process is set to the process with PID 1, which is "init". One of the jobs of "init" is to call wait function in a loop and thus reap any zombie process left behind by its parent.

### Practice: What do we need to do?
Variants to reap zombies were mentioned in theory part, but everything is related to C code. We can use POSIX layer wrapped via Mono, but .NET Framework already has good implementation of process handling using "Process" class. So everything looks like this:

```csharp
// Starting process somewhere
var process = Process.Start(new ProcessInfo { ... });
...
// Then wait it for exit
process.WaitForExit();
```
Specified "Process" implemented in Mono calls POSIX functions in UNIX/BSD OSes, so that's why we don't need to use POSIX layer. Anyway you can use POSIX function, but this is not neccessary, because of ready-for-production "Process" class in .NET Framework.

### Proof-of-Work: The program
This repository contains two projects: "TestPrimaryProcess" and "TestChildProcess".

TestPrimaryProcess is the parent process that is used to manipulate child processes. What can we do with it? Here is a list of available commands:
```
start
stop[-wait] [PID [=> PID2 => ... => PIDN]]
list
exit
```

Now with more detailed description of each one:
>> start - creates new process and informs used about newly created process: it's name and ID in table.

>> stop - signals to process or list of processes to stop it's execution. Plain stop signal sends SIGTERM to child process without waiting for SIGCHLD - so this can prove that zombie process leaves until SIGCHLD is catched by the parent process. By adding "-wait" post-fix to command is used to handle SIGCHLD by parent process. Another thing is list of child processes that can be passed. For example "stop-wait 1923 => 1927 => 1928" will make list of 1923, 1927, 1928 processes to be stopped and to be waited for them until SIGCHLD could be catched.

>> list - just lists all available child processes that were already created earlier. This is also the best way to see all needed processes to be selected to make list of processes to be stopped by stop command.

>> exit - stoppes all currently created child processes with waiting for SIGCHLD signal, then primary(e.g. parent) process stops.