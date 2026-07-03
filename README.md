Получить токен: https://oauth.yandex.ru/authorize?response_type=token&client_id=23cabbbdc6cd418abb4b39c32c41195d

```shell
dotnet publish -c Release -r win-x64 --self-contained
```

Запустить как сервис (после publish)
```shell
sc create SynchronousMp3WebPlayer binPath= "C:\Path\To\Your\Publish\Folder\YourAppName.exe"
sc start SynchronousMp3WebPlayer
```
