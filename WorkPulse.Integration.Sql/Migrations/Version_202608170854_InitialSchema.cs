using FluentMigrator;

namespace WorkPulse.Integration.Sql.Migrations;

[Migration(202608170854 )]
public sealed class Version_202608170854_InitialSchema : Migration
{               
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsString(64).PrimaryKey()
            .WithColumn("FirstName").AsString(100).NotNullable()
            .WithColumn("LastName").AsString(100).NotNullable()
            .WithColumn("Email").AsString(256).NotNullable()
            .WithColumn("UserName").AsString(256).NotNullable()
            .WithColumn("PasswordHash").AsString(int.MaxValue).NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable()
            .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);

        Create.Index("IX_Users_Email").OnTable("Users").OnColumn("Email").Ascending().WithOptions().Unique();

        Create.Table("Roles")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(50).NotNullable();

        Create.Index("IX_Roles_Name").OnTable("Roles").OnColumn("Name").Ascending().WithOptions().Unique();

        Create.Table("UserRoles")
            .WithColumn("UserId").AsString(64).NotNullable()
            .WithColumn("RoleId").AsInt32().NotNullable();

        Create.PrimaryKey("PK_UserRoles").OnTable("UserRoles").Columns("UserId", "RoleId");
        Create.ForeignKey("FK_UserRoles_Users_UserId")
            .FromTable("UserRoles").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");
        Create.ForeignKey("FK_UserRoles_Roles_RoleId")
            .FromTable("UserRoles").ForeignColumn("RoleId")
            .ToTable("Roles").PrimaryColumn("Id");

        Create.Table("Clients")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("ContactName").AsString(200).NotNullable()
            .WithColumn("ContactEmail").AsString(256).NotNullable()
            .WithColumn("PhoneNumber").AsString(50).NotNullable()
            .WithColumn("Description").AsString(1000).NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable()
            .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);

        Create.Index("IX_Clients_ContactEmail").OnTable("Clients").OnColumn("ContactEmail").Ascending().WithOptions().Unique();

        Create.Table("Projects")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ClientId").AsGuid().NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).NotNullable()
            .WithColumn("TotalTasks").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("StartDate").AsDateTime2().NotNullable()
            .WithColumn("EndDate").AsDateTime2().Nullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable();

        Create.ForeignKey("FK_Projects_Clients_ClientId")
            .FromTable("Projects").ForeignColumn("ClientId")
            .ToTable("Clients").PrimaryColumn("Id");
        Create.Index("IX_Projects_ClientId").OnTable("Projects").OnColumn("ClientId").Ascending();
        Create.Index("IX_Projects_Status").OnTable("Projects").OnColumn("Status").Ascending();

        Create.Table("Sprints")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ProjectId").AsGuid().NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("StartDate").AsDateTime2().NotNullable()
            .WithColumn("EndDate").AsDateTime2().NotNullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("TotalTasks").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable();

        Create.ForeignKey("FK_Sprints_Projects_ProjectId")
            .FromTable("Sprints").ForeignColumn("ProjectId")
            .ToTable("Projects").PrimaryColumn("Id");
        Create.Index("IX_Sprints_ProjectId").OnTable("Sprints").OnColumn("ProjectId").Ascending();
        Create.Index("IX_Sprints_Status").OnTable("Sprints").OnColumn("Status").Ascending();
        Create.Index("IX_Sprints_Dates").OnTable("Sprints").OnColumn("StartDate").Ascending().OnColumn("EndDate").Ascending();

        Create.Table("Tasks")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ProjectId").AsGuid().NotNullable()
            .WithColumn("SprintId").AsGuid().Nullable()
            .WithColumn("TaskType").AsInt32().NotNullable().WithDefaultValue(2)
            .WithColumn("AssignedUserId").AsString(64).Nullable()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).NotNullable()
            .WithColumn("StoryPoints").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("SprintOrder").AsInt32().Nullable()
            .WithColumn("DueDate").AsDateTime2().Nullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("Priority").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable()
            .WithColumn("CompletedAt").AsDateTime2().Nullable();

        Create.ForeignKey("FK_Tasks_Projects_ProjectId")
            .FromTable("Tasks").ForeignColumn("ProjectId")
            .ToTable("Projects").PrimaryColumn("Id");
        Create.ForeignKey("FK_Tasks_Sprints_SprintId")
            .FromTable("Tasks").ForeignColumn("SprintId")
            .ToTable("Sprints").PrimaryColumn("Id");
        Create.ForeignKey("FK_Tasks_Users_AssignedUserId")
            .FromTable("Tasks").ForeignColumn("AssignedUserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Index("IX_Tasks_ProjectId").OnTable("Tasks").OnColumn("ProjectId").Ascending();
        Create.Index("IX_Tasks_SprintId").OnTable("Tasks").OnColumn("SprintId").Ascending();
        Create.Index("IX_Tasks_SprintOrder").OnTable("Tasks").OnColumn("SprintOrder").Ascending();
        Create.Index("IX_Tasks_AssignedUserId").OnTable("Tasks").OnColumn("AssignedUserId").Ascending();
        Create.Index("IX_Tasks_DueDate").OnTable("Tasks").OnColumn("DueDate").Ascending();
        Create.Index("IX_Tasks_Status").OnTable("Tasks").OnColumn("Status").Ascending();
        Create.Index("IX_Tasks_Priority").OnTable("Tasks").OnColumn("Priority").Ascending();
    }

    public override void Down()
    {
        Delete.Table("Tasks");
        Delete.Table("Sprints");
        Delete.Table("Projects");
        Delete.Table("Clients");
        Delete.Table("UserRoles");
        Delete.Table("Roles");
        Delete.Table("Users");
    }
}
