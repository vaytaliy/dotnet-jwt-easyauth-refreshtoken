DELETE FROM UsersAndRoles WHERE 1=1;
DELETE FROM Roles WHERE 1=1;

INSERT INTO Roles (RoleName, RoleDescription)
VALUES 
	('adminchik', 'I can do anything I want'),
	('terminator', 'Hasta la vista, baby')
;

INSERT INTO UsersAndRoles(UserId, RoleName)
VALUES 
	(1, 'adminchik'),
	(1, 'terminator')
;
