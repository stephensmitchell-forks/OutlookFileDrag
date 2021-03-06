﻿using System;
using EasyHook;
using log4net;

namespace OutlookFileDrag
{
    class DragDropHook : IDisposable
    {
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private LocalHook hook;
        private bool disposed = false;
        private bool isHooked = false;

        public DragDropHook()
        {
            try
            {
                //Hook OLE drag and drop event
                log.Info("Creating hook for DoDragDrop method of ole32.dll");
                hook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("ole32.dll", "DoDragDrop"),
                    new NativeMethods.DragDropDelegate(DragDropHook.DoDragDropHook), null);
            }
            catch (Exception ex)
            {
                log.Error("Error creating hook", ex);
                throw;
            }
        }

        public bool IsHooked
        {
            get
            {
                return isHooked;
            }
        }

        public void StartHook()
        {
            try
            {
                if (isHooked)
                    return;

                log.Info("Starting hook");
                //Hook current (UI) thread
                hook.ThreadACL.SetInclusiveACL(new Int32[] { 0 });
                isHooked = true;
            }
            catch (Exception ex)
            {
                log.Error("Error starting hook", ex);
                throw;
            }
        }

        public void StopHook()
        {
            try
            {
                if (!isHooked)
                    return;

                log.Info("Stopping hook");
                //Stop hooking all threads
                hook.ThreadACL.SetInclusiveACL(new Int32[] { });
                isHooked = false;
                log.Info("Stopped hook");
            }
            catch (Exception ex)
            {
                log.Error("Error stopping hook", ex);
                throw;
            }
        }

        public static int DoDragDropHook(NativeMethods.IDataObject pDataObj, NativeMethods.IDropSource pDropSource, uint dwOKEffects, uint[] pdwEffect)
        {
            try
            {
                log.Info("Drag started");
                if (!DataObjectHelper.GetDataPresent(pDataObj, "FileGroupDescriptorW"))
                {
                    log.Info("No virtual files found -- continuing original drag");
                    return NativeMethods.DoDragDrop(pDataObj, pDropSource, dwOKEffects, pdwEffect);
                }

                //Start new drag
                log.Info("Virtual files found -- starting new drag adding CF_HDROP format");
                log.InfoFormat("Files: {0}", string.Join(",", DataObjectHelper.GetFilenames(pDataObj)));

                NativeMethods.IDataObject newDataObj = new OutlookDataObject(pDataObj);
                int result = NativeMethods.DoDragDrop(newDataObj, pDropSource, dwOKEffects, pdwEffect);

                //Get result
                log.InfoFormat("DoDragDrop result: {0}", result);
                return result;
            }
            catch (Exception ex)
            {
                log.Warn("Dragging error", ex);
                return NativeMethods.DRAGDROP_S_CANCEL;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                hook.Dispose();
            }

            disposed = true;
        }
    }

}
