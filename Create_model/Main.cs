using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using System.Reflection;

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

            Transaction transaction = new Transaction(doc, "Построекние стены");
            transaction.Start();

            List<Wall> walls = WallCreate(doc, level1, points, level2, width, length);

            AddDoor(doc, level1, walls[0]);
            AddWindows(doc, level1, walls[1]);
            AddWindows(doc, level1, walls[2]);
            AddWindows(doc, level1, walls[3]);
            AddRoof1(doc, level2, walls);

            transaction.Commit();

            return Result.Succeeded;
        }

        private void AddRoof1(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType=new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x=>x.Name.Equals("Типовой - 400мм"))
                .Where(x=>x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            double wallWight = walls[0].Width;
            double dt = wallWight / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));


            Application application = doc.Application;
            CurveArray footprint = application.Create.NewCurveArray();
            for (int i = 0; i < 4; i++)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                Line line = Line.CreateBound(p1 + points[i], p2+points[i+1]);
                footprint.Append(line);
            }
            ModelCurveArray footPrintToModelCurveMapping=new ModelCurveArray();
            FootPrintRoof footPrintRoof=doc.Create.NewFootPrintRoof(footprint,level2,roofType,out footPrintToModelCurveMapping);
            //ModelCurveArrayIterator iterator=footPrintToModelCurveMapping.ForwardIterator();
            //iterator.Reset();
            //while (iterator.MoveNext())
            //{
            //    ModelCurve modelCurve=iterator.Current as ModelCurve;
            //    footPrintRoof.set_DefinesSlope(modelCurve, true);
            //    footPrintRoof.set_SlopeAngle(modelCurve, 0.5);
            //}

            foreach ( ModelCurve m in footPrintToModelCurveMapping)
            {
                footPrintRoof.set_DefinesSlope(m, true);
                footPrintRoof.set_SlopeAngle(m, 0.5);
            }
        }
        private void AddRoof2(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            double wallWight = walls[0].Width;
            double dt = wallWight / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));


            Application application = doc.Application;
            CurveArray CurveArray = new CurveArray();
            XYZ[] pointsRef=new XYZ[0];
            int a = 0;


            for (int i = 0; i < 4; i += 2)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
                pointsRef.Append(p1 + points[a]);
                a += 2;
                CurveArray.Append(line);
            }
            ReferencePlane plane = doc.Create.NewReferencePlane(pointsRef[0], pointsRef[2]);
        }
        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType= new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            if(!doorType.IsActive)
                doorType.Activate();

            LocationCurve hostCurve= wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            doc.Create.NewFamilyInstance(point, doorType,wall,level1,StructuralType.NonStructural);
        }
        private void AddWindows(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowsType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0406 x 0610 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            if (!windowsType.IsActive)
                windowsType.Activate();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ pointOrigin = (point1 + point2) / 2;

            double pointX= ((point1 + point2) / 2).X;
            double pointY= ((point1 + point2) / 2).Y;
            double pointZ= ((point1 + point2) / 2).Z+ UnitUtils.ConvertToInternalUnits(1500, UnitTypeId.Millimeters);

           XYZ point = new XYZ(pointX, pointY, pointZ);


            doc.Create.NewFamilyInstance(point, windowsType, wall, level1, StructuralType.NonStructural);
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

            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }

            return walls;
        }
    }
}
