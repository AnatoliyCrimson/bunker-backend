import "../../styles/components/uikit/OverlayingPopup.scss"
import { useMount } from "../../hooks/useMount"
import Portal from "./Portal";
import { useEffect, useState } from "react";
import { RemoveScroll } from "react-remove-scroll";

function OverlayingPopup ({ children, onClose, isOpened, contentClassName }) {

    const { mounted } = useMount({ isOpened });
    
    useEffect(() => {
        function handleEscapeKey(event) {
            if (event.code === 'Escape') {
                onClose()
            }
        }

        document.addEventListener('keydown', handleEscapeKey)
        return () => document.removeEventListener('keydown', handleEscapeKey)
    }, [])


    if (!(isOpened || mounted)) {
        return null;
    }
    

    return (
        <Portal>
            <RemoveScroll>
                <div className="popup">
                    <div className={`popup__back ${mounted && isOpened ? 'popup__back-active' : ''}`} onClick={onClose} />            
                    <div className={`popup__content ${mounted && isOpened ? 'popup__content-active' : ''} ${contentClassName}`}>{children}</div> 
                </div>
            </RemoveScroll>
        </Portal>
    );
};

export default OverlayingPopup ;