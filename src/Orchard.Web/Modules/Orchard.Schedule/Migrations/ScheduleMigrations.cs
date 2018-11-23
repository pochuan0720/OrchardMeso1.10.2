using Orchard.Schedule.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using System.Data;

namespace Orchard.Schedule.Migrations
{
    [OrchardFeature("Orchard.Schedule")]
    public class ScheduleMigrations : DataMigrationImpl
    {
        public int Create()
        {
            // Creating table SchedulePartRecord
            SchemaBuilder.CreateTable("SchedulePartRecord", table => table
                .ContentPartRecord()
                .Column("StartDate", DbType.DateTime)
                .Column("EndDate", DbType.DateTime)
                .Column("Occurrences", DbType.Int32)
                .Column("ScheduleType", DbType.Int16)
                .Column("RepeatInterval", DbType.Int16)
                .Column("DaysOfWeek", DbType.Int16)
                .Column("DayOfMonth", DbType.Int16)
                .Column("WeekOfMonth", DbType.Int16)
                .Column("Month", DbType.Int16)
                .Column("AllDay", DbType.Boolean)
                .Column("StartTime", DbType.Int32)
                .Column("Duration", DbType.Int32)
                .Column("OffsetDays", DbType.Int32)
                .Column("TimeZone", DbType.String)
            );

            ContentDefinitionManager.AlterPartDefinition(
                typeof(SchedulePart).Name, cfg => cfg.Attachable());

            SchemaBuilder.CreateTable("ExcludedDateRecord", table => table
                .Column<int>("Id", column => column.PrimaryKey().Identity())
                .Column<int>("SchedulePartRecord_Id")
                .Column("Date", DbType.DateTime)
                );

            ContentDefinitionManager.AlterTypeDefinition("SimpleEvent", builder => builder
                .WithPart("SchedulePart")
                .WithPart("CommonPart")
                .WithPart("TitlePart")
                                    .WithPart("AutoroutePart", ctx => ctx
                        .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                        .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                        .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: 'Events/{Content.Slug}', Description: 'Events/simple-event'}]")
                        .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                .WithPart("TagsPart")
                .DisplayedAs("Simple Event")
                .Creatable()
                .Listable()
                .Draftable());

            return 8;
        }

        public int UpdateFrom1()
        {
            SchemaBuilder.CreateTable("ExcludedDateRecord", table => table
                .Column<int>("Id", column => column.PrimaryKey().Identity())
                .Column<int>("SchedulePartRecord_Id")
                .Column("Date", DbType.Date)
                );

            return 2;
        }

        public int UpdateFrom2()
        {
            SchemaBuilder.AlterTable("ExcludedDateRecord", table => table
                .AlterColumn("Date", c => c.WithType(DbType.DateTime))
                );

            return 3;
        }

        public int UpdateFrom3()
        {
            ContentDefinitionManager.AlterTypeDefinition("SimpleEvent", builder => builder
                .WithPart("SchedulePart")
                .WithPart("CommonPart")
                .WithPart("TitlePart")
                .WithPart("AutoroutePart", ctx => ctx
                    .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                    .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                    .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: 'Events/{Content.Slug}', Description: 'Events/simple-event'}]")
                    .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                .WithPart("TagsPart")
                .DisplayedAs("Simple Event")
                .Creatable());

            return 4;
        }

        public int UpdateFrom4()
        {
            SchemaBuilder.AlterTable("SchedulePartRecord", table => table
                .AddColumn<int>("Offset")
                );

            return 5;
        }

        public int UpdateFrom5()
        {
            // This won't work for SQL CE, but the name change is necessary for this module to work with CE. 

            // To update the database for CE, create a new int column named OffsetDays and drop the Offset column.
            // Finally, update the migration record for Orchard.Schedule.Migrations.ScheduleMigrations and set it to '6'.
            
            string prefix = SchemaBuilder.FormatPrefix(SchemaBuilder.FeaturePrefix);
            string tableName = prefix + "SchedulePartRecord";
            string sql = string.Format("sp_RENAME '{0}.Offset', 'OffsetDays', 'COLUMN'", tableName);
            SchemaBuilder.ExecuteSql(sql);

            return 6;
        }

        public int UpdateFrom6() {
            SchemaBuilder.AlterTable("SchedulePartRecord", table => table
                .AddColumn<string>("TimeZone"));

            return 7;
        }

        public int UpdateFrom7() {
            ContentDefinitionManager.AlterTypeDefinition("SimpleEvent", builder =>
                builder
                    .Listable()
                    .Draftable());

            return 8;
        }
    }
}