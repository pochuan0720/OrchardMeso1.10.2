using Orchard.Localization.Services;
using Orchard.MediaLibrary.MediaFileName;
using Orchard.MediaLibrary.Models;


namespace Orchard.MediaLibrary.Handlers
{
    public class UpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler, IUpdateModelHandler
    {
        public UpdateModelHandler(IDateLocalizationServices dateLocalizationServices) : base(dateLocalizationServices)
        {
        }

        public override bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            if (root != null)
            {
                dynamic _model = model;
                if (typeof(MediaPart) == model.GetType())
                {
                    if (root["Caption"] != null)
                        _model.Caption = root["Caption"].ToString();
                    if (root["AlternateText"] != null)
                        _model.AlternateText = root["AlternateText"].ToString();
                    if (root["FolderPath"] != null)
                        _model.FolderPath = root["FolderPath"].ToString();
                    /*foreach(string key in includeProperties)
                    {
                        if(root[key] != null)
                         _model.key = root[key].ToString();
                    }*/
                    return true;
                }
                else if (typeof(MediaFileNameEditorViewModel) == model.GetType() && root["FileName"] != null)
                {
                    _model.FileName = root["FileName"].ToString();
                    return true;
                }
                else
                {
                    return base.TryUpdateModel(model, prefix, includeProperties, excludeProperties);
                }
            }

            return false;
        }

        IUpdateModelHandler IUpdateModelHandler.SetData(object _root)
        {
            base.SetData(_root);
            return this;
        }
    }
}