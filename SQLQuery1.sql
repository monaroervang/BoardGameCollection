BEGIN TRANSACTION;

INSERT INTO BoardGameAuthDb.dbo.Users
    (Id, Username, PasswordHash, Role)
SELECT
    Id,
    Username,
    PasswordHash,
    Role
FROM BoardGameCollectionDb.dbo.Users;

COMMIT TRANSACTION;

SELECT COUNT(*) AS AntalBrugere
FROM BoardGameAuthDb.dbo.Users;