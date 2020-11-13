# DXVK Switch

This repo contains code to compile DXVK Switch application. DXVK Switch makes it possible to switch DXVK versions when starting a wine application and revert it back when it closes (without a user interrupt).  
This is originally intended for switching between async and sync versions of the DXVK, so I can run shader heavy games and programs with less stuttering.  
This application only works for Linux systems, support for Windows is not considered yet. Also this application is made for a super 

## Compilation

This repo currently doesn't offer a binary, instead you need to clone and compile the source yourself. Compilation and running requires .NET Core.  
Run the following commands to clone and compile the application.

```shell
$ git clone https://github.com/jangofett4/dxvk-switch
$ cd dxvk-switch
$ dotnet build
```

Executable output is in `bin/Debug/netcoreappX.Y/` replacing X.Y with .NET SDK installed (2.0, 3.1 etc).
To install the application I recommend creating a symlink to this path:

```shell
$ ln -sr ./bin/Debug/netcoreappX.Y/dxkv-switch /usr/bin/dxvksw
```

For testing if it works:

```
$ dxvksw
Usage: dxvk-switch version[/commands]
```

## Usage

To use the application one must install versions to use with this application. This application doesn't come pre-installed with DXVK binaries, you need to get them.  
Next to application executable there should be a empty `versions`, if not just create it. Switchable DXVK versions will be put here. Basic version folder structure is as follows:
```
versions
|
|-- 1.7.2
| |-- x64
| |-- x32
|
|-- 1.6.1
| |-- x64
| |-- x32
```

Normally a `dxvk-x.y.z.tar.gz` archive contains exactly correct format that this application needs, so a basic extract and copy should work in most cases.

After putting the versions into folder running the application without parameters should list installed versions, if information is correct move to next step.
If application doesn't show versions installed properly check the folder structure in versions folder.

Before running the application you should ensure that there is a default WINEPREFIX in your environment. If there is not you can export into current tty session with:
```shell
export WINEPREFIX=/path/to/your/wineprefix
```
To make this permanent consider adding previous code block to your `~/.bashrc` or `~/.zshrc`  
Basic usage of the application is as follows:
```shell
$ dxvk-switch version-x.y.z app [arg0 [, arg1 [, ...]]]
```

This will;
- Run switcher and switch to `version-x.y.z` if its present in `versions` directory (version-x.y.z string here is the folder name in versions directory, actual version string is not needed)
- Run application `app` with given arguments, this is generally wine or some other chain command
- When application is done switch back to original dlls

## Advanced Usage

This application provides some extra features to spice up some actions. For example you can insert pre or post run commands after `version` in command line:
```shell
$ dxvksw 1.7.2/! ...    # Will NOT revert after application is done, useful for permanently switching to versions. Destroys previous backup, use with caution
$ dxvksw /r      ...    # Will revert from a backup if there is one. Useful with '!' command
$ dxvksw 1.7.2/R ...    # Will reboot wine using
```

There are several commands that can be used:
- ! : Don't revert after application is done
- r : Revert from existing backup
- R : Reboot wine using wineboot before starting user application
- s : Shutdown wine using wineboot before starting user application
- u : Update wine prefix before starting user application

Commands can be chained like `1.7.2/rsu` for:
1. Revert from backup
2. Shutdown wine
3. Update wine prefix

## Help

Send me a googolplex doge coins