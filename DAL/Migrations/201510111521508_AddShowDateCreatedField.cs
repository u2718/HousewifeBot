namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddShowDateCreatedField : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Shows", "DateCreated", c => c.DateTimeOffset(precision: 7, defaultValueSql: "GetUtcDate()"));
            Sql("UPDATE dbo.Shows SET DateCreated = '03.08.2015'");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Shows", "DateCreated");
        }
    }
}
