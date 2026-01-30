import "../../styles/components/ui/Input.scss"


function Input({ value, onChange, className, ...props }) {
    
    
    
    
    return (
        <>
            <input
                className={"input " + className}
                value={value}
                onChange={onChange}
                {...props}
            />
        </>
    );
}

export default Input;