namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddShowNotificationsNavigationProperties : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ShowNotifications", "Show_Id", "dbo.Shows");
            DropForeignKey("dbo.ShowNotifications", "User_Id", "dbo.Users");
            DropIndex("dbo.ShowNotifications", new[] { "Show_Id" });
            DropIndex("dbo.ShowNotifications", new[] { "User_Id" });
            RenameColumn(table: "dbo.ShowNotifications", name: "Show_Id", newName: "ShowId");
            RenameColumn(table: "dbo.ShowNotifications", name: "User_Id", newName: "UserId");
            AlterColumn("dbo.ShowNotifications", "ShowId", c => c.Int(nullable: false));
            AlterColumn("dbo.ShowNotifications", "UserId", c => c.Int(nullable: false));
            CreateIndex("dbo.ShowNotifications", "UserId");
            CreateIndex("dbo.ShowNotifications", "ShowId");
            AddForeignKey("dbo.ShowNotifications", "ShowId", "dbo.Shows", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ShowNotifications", "UserId", "dbo.Users", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ShowNotifications", "UserId", "dbo.Users");
            DropForeignKey("dbo.ShowNotifications", "ShowId", "dbo.Shows");
            DropIndex("dbo.ShowNotifications", new[] { "ShowId" });
            DropIndex("dbo.ShowNotifications", new[] { "UserId" });
            AlterColumn("dbo.ShowNotifications", "UserId", c => c.Int());
            AlterColumn("dbo.ShowNotifications", "ShowId", c => c.Int());
            RenameColumn(table: "dbo.ShowNotifications", name: "UserId", newName: "User_Id");
            RenameColumn(table: "dbo.ShowNotifications", name: "ShowId", newName: "Show_Id");
            CreateIndex("dbo.ShowNotifications", "User_Id");
            CreateIndex("dbo.ShowNotifications", "Show_Id");
            AddForeignKey("dbo.ShowNotifications", "User_Id", "dbo.Users", "Id");
            AddForeignKey("dbo.ShowNotifications", "Show_Id", "dbo.Shows", "Id");
        }
    }
}
