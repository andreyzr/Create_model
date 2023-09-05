using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create_model
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            List<XYZ> points = new List<XYZ>();


            Level level1 = GetLevel(doc,"Уровень 1");
            Level level2 = GetLevel(doc,"Уровень 2");


            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double length = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx=width/2;
            double dy =length / 2;

        
            points.Add(new XYZ(-dx, -dy,0));
            points.Add(new XYZ(dx, -dy,0));
            points.Add(new XYZ(dx, dy,0));
            points.Add(new XYZ(-dx, dy,0));
            points.Add(new XYZ(-dx, -dy,0));

            List<Wall> walls = WallCreate(doc, level1, points, level2, width, length);


            return Result.Succeeded;
        }


        public static Level GetLevel(Document doc, string name)
        {

            List<Level> listLevel = new FilteredElementCollector(doc)
             .OfClass(typeof(Level))
             .OfType<Level>()
             .ToList();

            Level level = listLevel
                 .Where(x => x.Name.Equals(name))
                 .FirstOrDefault();


            return level;
        }

        public static List<Wall> WallCreate (Document doc, Level level1,List<XYZ> points, Level level2, double width, double length )
        {

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построекние стены");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            transaction.Commit();

            return walls;
        }
    }
}
