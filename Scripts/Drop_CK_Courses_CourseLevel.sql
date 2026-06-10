/* Removes CK_Courses_CourseLevel so CourseLevel values come from dbo.Configurations (ConfigKey = CourseLevel). */
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Courses_CourseLevel'
      AND parent_object_id = OBJECT_ID(N'dbo.Courses'))
BEGIN
    ALTER TABLE dbo.Courses DROP CONSTRAINT CK_Courses_CourseLevel;
END
GO
