namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSettings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Settings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        WebUiUrl = c.String(),
                        WebUiPassword = c.String(),
                        SiteLogin = c.String(),
                        SitePassword = c.String(),
                        AutoDownload = c.Boolean(nullable: false),
                        User_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.User_Id)
                .Index(t => t.User_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Settings", "User_Id", "dbo.Users");
            DropIndex("dbo.Settings", new[] { "User_Id" });
            DropTable("dbo.Settings");
        }
    }
}
