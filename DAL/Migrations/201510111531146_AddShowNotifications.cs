namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddShowNotifications : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ShowNotifications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Notified = c.Boolean(nullable: false),
                        Show_Id = c.Int(),
                        User_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Shows", t => t.Show_Id)
                .ForeignKey("dbo.Users", t => t.User_Id)
                .Index(t => t.Show_Id)
                .Index(t => t.User_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ShowNotifications", "User_Id", "dbo.Users");
            DropForeignKey("dbo.ShowNotifications", "Show_Id", "dbo.Shows");
            DropIndex("dbo.ShowNotifications", new[] { "User_Id" });
            DropIndex("dbo.ShowNotifications", new[] { "Show_Id" });
            DropTable("dbo.ShowNotifications");
        }
    }
}
