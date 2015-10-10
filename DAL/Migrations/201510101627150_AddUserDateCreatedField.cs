namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserDateCreatedField : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "DateCreated", c => c.DateTimeOffset(precision: 7, defaultValueSql: "GetUtcDate()"));
            Sql("UPDATE dbo.Users SET DateCreated = '03.08.2015'");
        }

        public override void Down()
        {
            DropColumn("dbo.Users", "DateCreated");
        }
    }
}
