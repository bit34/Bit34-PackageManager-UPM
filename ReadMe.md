# **Bit34 - Unity Package Manager**

# **Table of contents**
- [What is it?](#what-is-it)
- [Setup](#setup)
    - [Add Package Manager](#add-package-manager)
    - [Define your packages](#define-your-packages)
    - [Add your dependencies](#add-your-dependencies)
    - [Load your packages](#load-your-packages)

## **What is it?**
Unity does not support packages with git dependencies and this prevents you from hosting all your packages on Github. This tool is a temporary solution until Unity implementes this.

#### IMPORTANT
1 - Package uses git commands for retrieving your packages. Make sure all your packages are accessible from your computer with git(git is installed, ssh keys are set for private packages).

2 - Do not forget to add this two lines to your .gitignore file to prevent your project's git interfering with packages.
- Assets/Bit34/Packages/
- Assets/Bit34/Packages.meta


## **Setup**

### **Add Package Manager**

You can either open Unity Package Manager, click plus button, add this url to textbox
```
https://github.com/bit34/Bit34-PackageManager-UPM.git
```

Or add following line directly to your Packages/manifest.json file

```
"com.bit34games.packagemanager": "https://github.com/bit34/Bit34-PackageManager-UPM.git",
```
### **Define your packages**

Save your package list file at ```Assets/Bit34/repositories.json``` under your unity project 

An example file is given below:

```
{
    "packages" : [
        {
            "name" : "com.bit34games.injector",
            "url" : "git@github.com:bit34/Bit34-Injector-UPM.git"
        },
        {
            "name" : "com.bit34games.director",
            "url" : "git@github.com:bit34/Bit34-Director-UPM.git"
        },
        {
            "name" : "com.bit34games.time",
            "url" : "git@github.com:bit34/Bit34-TimeManager-UPM.git"
        },
        {
            "name" : "com.bit34games.graph",
            "url" : "git@github.com:bit34/Bit34-Graph-UPM.git"
        }
    ]
}
```

### **Add your dependencies**

Save your dependencies to ```Assets/Bit34/dependencies.json``` under your unity project.

An example is given below:

```
{
    "dependencies":
    {
        "com.bit34games.director": "1.2.0"
    }
}
```

### **Load your packages**

Now you can open Bit34 Package Manager from *Bit34* button from top bar.

Everytime you modify the ```Assets/Bit34/dependencies.json``` press *Reload* button on package manager window (and wait a little for changes to take affect)
