PUBLISH_DIR =  ENV['PUBLISH_DIR']  || "Release" 
SOLUTION =     ENV['SOLUTION']     || "LoadModules.Extensions.sln" 
DBSERVER =     ENV['DBSERVER']     || "localhost//sqlexpress" 
DBPREFIX =     ENV['DBPREFIX']     || "EXT_" 
MYSQLDB  =     ENV['MYSQLDB']      || "adp-hicci-03"
MYSQLUSR =     ENV['MYSQLUSR']     || "hicci"
MYSQLPASS=     ENV['MYSQLPASS']    || "killerzombie"
PRERELEASE =   ENV['PRERELEASE']   || "false"
CANDIDATE =    ENV['CANDIDATE']    || "false"
SUFFIX =       ENV['SUFFIX']       || "develop"
NUGETKEY =     ENV['NUGETKEY']     || "blahblahblahbroken!"
MSBUILD15CMD = ENV['MSBUILD15CMD'] || "C:/Program Files (x86)/Microsoft Visual Studio/2017/BuildTools/MSBuild/15.0/Bin/msbuild.exe"
SQUIRREL =     ENV['SQUIRREL']     || "C:/Users/ltramma/.nuget/packages/squirrel.windows/1.9.1/tools/Squirrel.exe"
GITHUB =       ENV['GITHUB']       || "4d286e77faef2f148c74a93c2666e948c48080f7"
RDMP_TOOLS =     ENV['RDMP_TOOLS'] || "%UserProfile%/.nuget/packages/hic.rdmp.plugin/3.0.12-rc/tools/netcoreapp2.2/publish/"