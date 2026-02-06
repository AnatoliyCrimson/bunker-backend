import { useEffect, useState } from "react"

export const useMount = ({ isOpened }) => {
    const [mounted, setMounted] = useState(false);

    useEffect(() => {
        let timer;
        if (isOpened && !mounted) {
            setMounted(true);
        } else if (!isOpened && mounted) {
            timer = setTimeout(() => {
                setMounted(false)
            }, 1000)
        }

        return () => {
            clearTimeout(timer);
        }
    }, [isOpened, mounted])


    return {
        mounted,
    };
};