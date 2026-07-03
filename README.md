Получить токен: https://oauth.yandex.ru/authorize?response_type=token&client_id=23cabbbdc6cd418abb4b39c32c41195d

Пример `.env` для HTTPS:

```dotenv
TOKEN_VLAD=y0__*************************************************
TOKEN_ELVIR=y0__*************************************************
TOKEN_MAKAR=y0__*************************************************
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://0.0.0.0:7289;http://0.0.0.0:5123
HTTPS_PORT=7289
ASPNETCORE_Kestrel__Certificates__Default__Path=certs/synchronous-player.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=change-me
```

```shell
dotnet publish -c Release -r win-x64 --self-contained
```

Запустить как сервис (после publish)
```powershell
sc create SynchronousMp3WebPlayer binPath= "F:\SynchronousMp3WebPlayer\SynchronousMp3WebPlayer.exe"
sc start SynchronousMp3WebPlayer
```

После запуска основной адрес:

```text
https://localhost:7289
```
