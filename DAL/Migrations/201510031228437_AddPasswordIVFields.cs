namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPasswordIVFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Settings", "WebUiPasswordIV", c => c.String());
            AddColumn("dbo.Settings", "SitePasswordIV", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Settings", "SitePasswordIV");
            DropColumn("dbo.Settings", "WebUiPasswordIV");
        }
    }
}
