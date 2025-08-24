public static void  Animate(FamilyInstance familyInstance, Curve curve)
{
    try
    {
        Document doc = familyInstance.Document;
        UIDocument uidoc = new UIDocument(doc);

        if (curve == null) return;

        List<XYZ> points = new List<XYZ>();
        if (curve is Line)
        {
            long segmentCount = (long)(curve.Length);
            points = Enumerable.Range(0, (int)segmentCount + 1).Select(i => curve.Evaluate(i / curve.Length, true)).ToList();
        }
        else
        {
            points = curve.Tessellate().ToList();
        }

        using (TransactionGroup tg = new TransactionGroup(doc, "name"))
        {
            try
            {
                tg.Start();
                foreach (XYZ p in points)
                {
                    using (Transaction t = new Transaction(doc, "name"))
                    {
                        try
                        {
                            t.Start();
                            ElementTransformUtils.MoveElement(doc, familyInstance.Id, p - ((LocationPoint)familyInstance.Location).Point);
                            t.Commit();
                        }
                        catch (Exception e)
                        {
                            TaskDialog.Show("Error!", e.Message);
                            return;
                        }
                    }



                    using (Transaction t = new Transaction(doc, "name"))
                    {
                        try
                        {
                            t.Start();
                            double rawParameter = curve.Project(p).Parameter;
                            Transform transform = curve.ComputeDerivatives(curve.ComputeNormalizedParameter(rawParameter), true);
                            Line rotationAxis = Line.CreateBound(p, p + new XYZ(0, 0, 1));
                            Transform familyInstanceTransform = familyInstance.GetTransform();
                            double angle = familyInstanceTransform.BasisX.AngleOnPlaneTo(transform.BasisX, XYZ.BasisZ);
                            ElementTransformUtils.RotateElement(doc, familyInstance.Id, rotationAxis, angle);
                            Thread.Sleep(1000 / 32);
                            uidoc.RefreshActiveView();
                            uidoc.UpdateAllOpenViews();

                            t.Commit();
                        }
                        catch (Exception e)
                        {
                            TaskDialog.Show("Error!", e.Message);
                            return;
                        }
                    }
                }
                tg.Assimilate();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Error!", e.Message);
                return;
            }
        }
    }
    catch (Exception e)
    {
        TaskDialog.Show("Error!", e.Message);
        return;
    }
}
