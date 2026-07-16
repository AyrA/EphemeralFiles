# Ephemeral Files Library

This is a .NET library for ephemeral files.

Files that are marked as ephemeral are cleaned up even if the application crashes.

## Usage

Create an instance of `EphemeralFileStream` and use it like a regular stream.
You can specify whether you want to start with the file being ephemeral already,
or as a regular file.

Normally you create a new file, but one constructor overload allows you to specify an existing file.
Doing that while also specifying that said file should be initially ephemeral immediately makes the file ephemeral.

## Compatibility

This stream derives from the `FileStream` class, and thus can be used in almost all cases a regular file stream is used for.

Note however that the file pointed to by the `Name` property doesn't actually exists if the file is currently ephemeral.
Some components might not deal well with that fact.

## Check ephemeral mode

Use the `.IsEphemeral` property to check if the file is currently ephemeral or not.

## Set ephemeral mode

The stream has `.Restore()` and `.MakeEphemeral()` methods to allow you to switch between modes after the stream has been created.
This works even after data has already been written.

The restore method also comes in an async variant because restoring an ephemeral file is not an instant operation.
To restore an ephemeral file you need enough free space available to fit the current stream twice for a brief moment.

## File handles

Whenever you call `.Restore()` the underlying file handle will change.
Be careful when you hold onto handles or pass them around, because the old handle will become invalid.

## Disposing

If you dispose of the stream and the file is currently not ephemeral it will stay on disk.

## System crash

If the system itself crashes, there is no guarantee that the ephemeral file will be cleaned up,
or when it happens.
Most operating systems will free any remaining allocated blocks during the next file system scan.

Therefore I recommend you run a file system check after a system crash.