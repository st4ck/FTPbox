FTPbox
=============

About
--------------

Synchronize your files with your own server, via FTP, FTPS or SFTP. Learn more on [ftpbox.org][website]

### Latest release
- Latest release is [v2.6.4][latestrelease] updated on 21/10/2017

### Upgrade

- Project upgraded to .NET Framework 4.5.2
- Upgraded to new FluentFTP library compiled from sources
- Using latest SSH.Net, Newtonsoft.Json, Ionic.Zip

### Major Changes
- Removing almost all async methods, the main function (check files local/remote) will run in a thread to avoid interface locking
- Adding remote listing state when starting
- Modify FluentFTP sources to avoid certain type of Exceptions and force Binary type in FTP SIZE command
- So FluentFTP is now integrated with FTPbox sources. Allows easier and faster debug and better customization

### Main Features

- Connect using FTP, SFTP or FTPS
- Share your files with direct links to them.
- Manage your files in-browser with the [Web Interface][webUI]
- Manage specific files/folders with the Context Menus
- Selective Sync
- Manual or Automatic synchronizing
- Bandwidth control
- Multiple Profiles
- Offline Mode

### Testing

- Tested intensively with ProFTPD 1.3.6 and 1.3.7rc1 (compiled from sources) with or without SSL and TLS
- Tested with sshd (OpenSSH_7.2p2)

### Advise

- Use if possibile FTPS or SFTP; FTP has plain text authentication, not a good idea if you transfer important documents
- Use if possible latest TLSv1.2 encryption protocol 
- When configure your FTP server pay attention to change Size and Timeout Data/Connection transfer. Otherwise if the file is big FTPbox was unable to end uploading
Example: in Proftpd conf add/change TimeoutNoTransfer, TimeoutStalled, TimeoutIdle, MaxStoreFileSize

### License

FTPbox is licensed under the [General Public License v3][gpl]. See [LICENSE][license] for the full text.

### Acknowledgements

FTPbox uses the following awesome libraries:
- [FluentFTP][fluentftp] : The FluentFTP library
- [SSH.NET][sshnet] : The SFTP library
- [Json.NET][jsonnet] : The json library used for the configuration file
- [DotNetZip][dotnetzip] : The library used for unzipping archives

Development
--------------

### To-Do

You can find the to-do list on [trello.com][todo]

### Get in touch

I'd love to hear from you. Please send your emails to support@ftpbox.org

Support
--------------

### Reporting

See [Feedback](https://github.com/FTPbox/FTPbox/wiki/Feedback)

### Translate

See [Translating](https://github.com/FTPbox/FTPbox/wiki/Translating)

### Donate

You can show the project some love by making a donation! You can find out how to donate from the [About][abt] page.

[website]: http://ftpbox.org
[webUI]: https://github.com/FTPbox/Web-Interface
[gpl]: http://www.tldrlegal.com/license/gnu-general-public-license-v3-(gpl-3)
[license]: https://github.com/FTPbox/FTPbox/blob/master/LICENSE
[todo]: https://trello.com/board/ftpbox/515afda9a23fa0b412001067
[abt]: http://ftpbox.org/about/
[fluentftp]: https://github.com/robinrodricks/FluentFTP/tree/master/FluentFTP/
[sshnet]: http://sshnet.codeplex.com/
[jsonnet]: http://json.codeplex.com/
[dotnetzip]: http://dotnetzip.codeplex.com/
[latestrelease]: https://github.com/st4ck/FTPbox/releases/tag/v2.6.4
