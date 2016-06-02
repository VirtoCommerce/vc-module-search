set V=2.10.4
nuget push VirtoCommerce.SearchModule.Client.%V%.nupkg -Source nuget.org -ApiKey %1
nuget push VirtoCommerce.SearchModule.Data.%V%.nupkg -Source nuget.org -ApiKey %1
pause
