import { useState } from "react"
import "../../styles/components/ui/Copy.scss"

function Copy({ text, containerClass, textClass, descrClass }) {



    const [isCopied, setIsCopied] = useState(false)
    const [copiedError, setCopiedError] = useState("")

    const handleCopy = () => {
        if (isCopied) return;
        navigator.clipboard.writeText(text)
            .then(() => {
                setIsCopied(true)

                setTimeout(() => {
                    setIsCopied(false)
                }, 1000)    
            })
            .catch(error => {
                console.error("Ошибка копирования:", error);
                setCopiedError(error)
            })
    }


    return (
        <>
            <div onClick={handleCopy} className={"copy__container " + containerClass}>
                <p className={"copy__text " + textClass}>
                    { text }
                </p>
                <p className={"copy__descr " + descrClass}>Нажмите что бы скопировать</p>
                <p className={`copy__result copy__result--success ${isCopied ? 'copy__result--active' : ''}`}>Скопировано</p>
                <p className={`copy__result copy__result--error ${copiedError ? 'copy__result--active' : ''}`}>Ошибка при копировании</p>
            </div>
        </>
    );
}

export default Copy;