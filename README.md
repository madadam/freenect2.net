Freenect2.Net
=============

A work-in-progress C# wrapper for [libfreenect2](https://github.com/OpenKinect/libfreenect2), tested on Ubuntu 14.04.

Note this library only works with Kinect One. If you have Kinect 360, you should
use [libfreenect](http://openkinect.org/wiki/Main_Page) which comes with its own
C# wrapper (as well as many other wrappers).

This repo was forked from https://github.com/madadam/freenect2.net

## Bulid ##

After cloning the repo _cd_ into the _freenect.net_ folder and follow these build steps: 

1. Expose the _libfreenect2_ library and include paths: 

		source setenv.sh
				
	It does nothing else than these two exports:
	
		export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:~/freenect2/lib
		export CPLUS_INCLUDE_PATH=$CPLUS_INCLUDE_PATH:~/freenect2/include
				
	If you have installed the freenect2 files elsewhere, replace ~/freenect2/include with it. 
		
2. _make_ the _libfreenect2c.so_ in the _native_ folder:	
	
		cd native
		make
		cp libfreenect2c.so ..
		cd ..

3. Copy the _libfreenect2_ libraries into the base folder.
   If you have done a _make install_ of the _libfreenect2_ to the default ~/freenect2 folder, this would look like
   
		cp ~/freenect2/lib/libfreenect2.so* . 
		
4. Finally, start the solution from the same bash, so C# can find the library via $LD_LIBRARY_PATH (here using MonoDevelop as development environment):
    	
		monodevelop Freenect2.sln
		
5. Check the target framework of the two solution subprojects to match your currently installed .NET version.

6. Press F5
