window.svgEditorCanvas = (function () {
    let dotNetRef = null;
    let svgElement = null;
    let isDragging = false;
    let isFencing = false;
    let isResizing = false;
    let resizeHandle = null;
    let lastSvgPoint = null;

    function getSvgPoint(evt) {
        const pt = svgElement.createSVGPoint();
        pt.x = evt.clientX;
        pt.y = evt.clientY;
        const ctm = svgElement.getScreenCTM();
        if (!ctm) return null;
        return pt.matrixTransform(ctm.inverse());
    }

    function onMouseDown(evt) {
        let target = evt.target;

        // Check if the click is on a resize handle
        if (target.getAttribute && target.getAttribute('data-resize-handle')) {
            evt.preventDefault();
            isResizing = true;
            isDragging = false;
            isFencing = false;
            resizeHandle = target.getAttribute('data-resize-handle');
            lastSvgPoint = getSvgPoint(evt);
            dotNetRef.invokeMethodAsync('OnResizeHandleMouseDown', resizeHandle,
                lastSvgPoint ? lastSvgPoint.x : 0,
                lastSvgPoint ? lastSvgPoint.y : 0);
            return;
        }

        // Walk up from the click target to find the nearest element with a data-element-id.
        // Elements inside raw markup (e.g. <defs> children) won't have one,
        // so we skip them until we reach a managed element or the SVG root.
        while (target && target !== svgElement) {
            if (target.getAttribute && target.getAttribute('data-element-id')) break;
            target = target.parentElement;
        }
        const elementId = target && target !== svgElement
            ? target.getAttribute('data-element-id')
            : null;

        lastSvgPoint = getSvgPoint(evt);
        const ctrlKey = evt.ctrlKey || evt.metaKey;

        if (elementId && !ctrlKey) {
            evt.preventDefault();
            isDragging = true;
            isFencing = false;
            dotNetRef.invokeMethodAsync('OnElementMouseDown', elementId,
                lastSvgPoint ? lastSvgPoint.x : 0,
                lastSvgPoint ? lastSvgPoint.y : 0,
                ctrlKey);
        } else if (elementId) {
            // Ctrl+click on element: toggle selection and start potential fence
            evt.preventDefault();
            isDragging = false;
            isFencing = true;
            dotNetRef.invokeMethodAsync('OnElementMouseDown', elementId,
                lastSvgPoint ? lastSvgPoint.x : 0,
                lastSvgPoint ? lastSvgPoint.y : 0,
                ctrlKey);
        } else if (target === svgElement || target === evt.target) {
            evt.preventDefault();
            isDragging = false;
            isFencing = true;
            dotNetRef.invokeMethodAsync('OnCanvasMouseDown',
                lastSvgPoint ? lastSvgPoint.x : 0,
                lastSvgPoint ? lastSvgPoint.y : 0,
                ctrlKey);
        }
    }

    function onMouseMove(evt) {
        if (!isDragging && !isFencing && !isResizing) return;
        if (!lastSvgPoint) return;
        evt.preventDefault();
        const pt = getSvgPoint(evt);
        if (!pt) return;
        const dx = pt.x - lastSvgPoint.x;
        const dy = pt.y - lastSvgPoint.y;
        lastSvgPoint = pt;
        if (isResizing) {
            dotNetRef.invokeMethodAsync('OnResizeMouseMove', dx, dy, pt.x, pt.y);
        } else {
            dotNetRef.invokeMethodAsync('OnMouseMove', dx, dy, pt.x, pt.y);
        }
    }

    function onMouseUp(evt) {
        if (!isDragging && !isFencing && !isResizing) return;
        const pt = getSvgPoint(evt);
        const wasResizing = isResizing;
        isDragging = false;
        isFencing = false;
        isResizing = false;
        resizeHandle = null;
        lastSvgPoint = null;
        if (wasResizing) {
            dotNetRef.invokeMethodAsync('OnResizeMouseUp');
        } else {
            dotNetRef.invokeMethodAsync('OnMouseUp',
                pt ? pt.x : 0,
                pt ? pt.y : 0);
        }
    }

    function onKeyDown(evt) {
        if (!dotNetRef) return;
        var tag = evt.target && evt.target.tagName;
        if (tag === 'INPUT' || tag === 'TEXTAREA' || (evt.target && evt.target.isContentEditable)) return;
        var ctrl = evt.ctrlKey || evt.metaKey;
        if (ctrl && evt.key === 'z' && !evt.shiftKey) {
            evt.preventDefault();
            dotNetRef.invokeMethodAsync('OnKeyDown', 'undo');
        } else if (ctrl && (evt.key === 'y' || (evt.key === 'z' && evt.shiftKey))) {
            evt.preventDefault();
            dotNetRef.invokeMethodAsync('OnKeyDown', 'redo');
        }
    }

    return {
        initCanvas: function (ref, svg) {
            dotNetRef = ref;
            svgElement = svg;
            svg.addEventListener('mousedown', onMouseDown);
            window.addEventListener('mousemove', onMouseMove);
            window.addEventListener('mouseup', onMouseUp);
            window.addEventListener('keydown', onKeyDown);
        },
        dispose: function () {
            if (svgElement) {
                svgElement.removeEventListener('mousedown', onMouseDown);
                svgElement = null;
            }
            window.removeEventListener('mousemove', onMouseMove);
            window.removeEventListener('mouseup', onMouseUp);
            window.removeEventListener('keydown', onKeyDown);
            dotNetRef = null;
            isDragging = false;
            isFencing = false;
            isResizing = false;
            resizeHandle = null;
            lastSvgPoint = null;
        }
    };
})();
