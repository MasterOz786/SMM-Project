using SMM.Core.Security;

var pwd = args.Length > 0 ? args[0] : "Student123";
for (var i = 0; i < 12; i++)
    Console.WriteLine(PasswordHasher.Hash(pwd));
