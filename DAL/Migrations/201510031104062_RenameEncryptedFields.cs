namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameEncryptedFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Settings", "_WebUiPassword", c => c.String());
            AddColumn("dbo.Settings", "_SitePassword", c => c.String());
            DropColumn("dbo.Settings", "WebUiPassword");
            DropColumn("dbo.Settings", "SitePassword");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Settings", "SitePassword", c => c.String());
            AddColumn("dbo.Settings", "WebUiPassword", c => c.String());
            DropColumn("dbo.Settings", "_SitePassword");
            DropColumn("dbo.Settings", "_WebUiPassword");
        }
    }
}
