require "net/http"
require 'uri'
require 'json'
require 'rexml/document'

load 'rakeconfig.rb'
$MSBUILD15CMD = MSBUILD15CMD.gsub(/\\/,"/")

task :ci_low_warnings, [:config,:level] => [:assemblyinfo, :build_low_warning]

task :ci_continuous, [:config] => [:setup_connection, :assemblyinfo, :build, :tests]

task :ci_integration, [:config] => [:setup_connection, :assemblyinfo, :build, :all_tests]

task :plugins, [:config] => [:assemblyinfo, :build_release, :deployplugins]

task :release => [:assemblyinfo, :build_release]

task :tests, [:config] => [:run_unit_tests]

task :all_tests, [:config] => [:createtestdb, :run_all_tests]

task :restorepackages do
    sh "nuget restore #{SOLUTION}"
end

task :setup_connection do 
    File.open("LoadModules.Extensions.Tests/TestDatabases.txt", "w") do |f|
        f.write "ServerName: #{DBSERVER}\r\n"
        f.write "Prefix: #{DBPREFIX}\r\n"
        f.write "MySql: Server=#{MYSQLDB};Uid=#{MYSQLUSR};Pwd=#{MYSQLPASS};SslMode=Required\r\n"
    end
end

task :build, [:config] => :restorepackages do |msb, args|
	sh "\"#{$MSBUILD15CMD}\" #{SOLUTION} \/t:Clean;Build \/p:Configuration=#{args.config}"
end

task :build_release => :restorepackages do
	sh "\"#{$MSBUILD15CMD}\" #{SOLUTION} \/t:Clean;Build \/p:Configuration=Release"
end

task :build_low_warning, [:config,:level] => :restorepackages do |msb, args|
	args.with_defaults(:level => 1)
	sh "\"#{$MSBUILD15CMD}\" #{SOLUTION} \/t:Clean;Build \/p:Configuration=#{args.config} \/p:WarningLevel=#{args.level} \/p:TreatWarningsAsErrors=false"
end

task :createtestdb, [:config] do |t, args|
	userProf = ENV['USERPROFILE'].gsub(/\\/,"/")	
	rdmpversion = getrdmpversion()
		
	RDMP_TOOLS = "#{userProf}/.nuget/packages/hic.rdmp.plugin/#{rdmpversion}/tools/netcoreapp2.2/publish/"
	Dir.chdir("#{RDMP_TOOLS}") do
        sh "dotnet ./rdmp.dll install #{DBSERVER} #{DBPREFIX} -D"
    end
end

task :run_unit_tests do 
	sh 'dotnet test --no-build --filter TestCategory=Unit --logger:"nunit;LogFilePath=test-result.xml"'
end

task :run_all_tests do 
	sh 'dotnet test --no-build --logger:"nunit;LogFilePath=test-result.xml"'
end

desc "Sets the version number from SharedAssemblyInfo file"    
task :assemblyinfo do 
	asminfoversion = File.read("SharedAssemblyInfo.cs").match(/AssemblyInformationalVersion\("(\d+)\.(\d+)\.(\d+)(-.*)?"/)
    
	puts asminfoversion.inspect
	
    major = asminfoversion[1]
	minor = asminfoversion[2]
	patch = asminfoversion[3]
    suffix = asminfoversion[4]
	
	version = "#{major}.#{minor}.#{patch}"
    puts "version: #{version}#{suffix}"
    
	# DO NOT REMOVE! needed by build script!
    f = File.new('version', 'w')
    f.write "#{version}#{suffix}"
    f.close
    # ----
end

desc "Pushes the plugin packages to nuget.org"    
task :deployplugins, [:config] do |t, args|
	version = File.open('version') {|f| f.readline}
    puts "version: #{version}"
	
	Dir.chdir('Plugin/netcoreapp2.2/') do
		sh "dotnet publish --runtime win-x64 -c #{args.config}"
	end
	#Packages the plugin which will be loaded into RDMP
	sh "nuget pack HIC.Extensions.nuspec -Properties Configuration=#{args.config} -IncludeReferencedProjects -Symbols -Version #{version}"
end

def getrdmpversion()
	document = REXML::Document.new File.new("Python/LoadModules.Extensions.Python/LoadModules.Extensions.Python.csproj")
	document.elements.each("*/ItemGroup/PackageReference") do |element|
		if element.attributes.get_attribute("Include").value == "HIC.RDMP.Plugin"
			return element.attributes.get_attribute("Version").value
		end
	end
end