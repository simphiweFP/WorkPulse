using FluentMigrator;

namespace WorkPulse.Integration.Sql.Migrations;

[Migration(202611130001)]
public sealed class Version_202611130001_InitialSchema : Migration
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
            .WithColumn("Email").AsString(256).NotNullable()
            .WithColumn("Phone").AsString(50).NotNullable()
            .WithColumn("Address").AsString(1000).NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable();

        Create.Index("IX_Clients_Email").OnTable("Clients").OnColumn("Email").Ascending().WithOptions().Unique();

        Create.Table("Projects")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ClientId").AsGuid().NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).NotNullable()
            .WithColumn("StartDate").AsDateTime2().NotNullable()
            .WithColumn("EndDate").AsDateTime2().Nullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable();

        Create.ForeignKey("FK_Projects_Clients_ClientId")
            .FromTable("Projects").ForeignColumn("ClientId")
            .ToTable("Clients").PrimaryColumn("Id");
        Create.Index("IX_Projects_ClientId").OnTable("Projects").OnColumn("ClientId").Ascending();

        Create.Table("Tasks")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ProjectId").AsGuid().NotNullable()
            .WithColumn("AssignedUserId").AsString(64).Nullable()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).NotNullable()
            .WithColumn("DueDate").AsDateTime2().Nullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("Priority").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable()
            .WithColumn("CompletedAt").AsDateTime2().Nullable();

        Create.ForeignKey("FK_Tasks_Projects_ProjectId")
            .FromTable("Tasks").ForeignColumn("ProjectId")
            .ToTable("Projects").PrimaryColumn("Id");
        Create.ForeignKey("FK_Tasks_Users_AssignedUserId")
            .FromTable("Tasks").ForeignColumn("AssignedUserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Index("IX_Tasks_ProjectId").OnTable("Tasks").OnColumn("ProjectId").Ascending();
        Create.Index("IX_Tasks_AssignedUserId").OnTable("Tasks").OnColumn("AssignedUserId").Ascending();
        Create.Index("IX_Tasks_DueDate").OnTable("Tasks").OnColumn("DueDate").Ascending();
        Create.Index("IX_Tasks_Status").OnTable("Tasks").OnColumn("Status").Ascending();
    }

    public override void Down()
    {
        Delete.Table("Tasks");
        Delete.Table("Projects");
        Delete.Table("Clients");
        Delete.Table("UserRoles");
        Delete.Table("Roles");
        Delete.Table("Users");
    }
}
