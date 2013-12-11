FieldGroupBinding.prototype = new Binding;
FieldGroupBinding.prototype.constructor = FieldGroupBinding;
FieldGroupBinding.superclass = Binding.prototype;

FieldGroupBinding.ACTION_HIDE = "fieldgrouphide";
FieldGroupBinding.CLASSNAME_NOLABEL = "nolabel";
FieldGroupBinding.CLASSNAME_FIRST = "first"; // attached by FieldsBinding!

/**
 * @class
 */
function FieldGroupBinding () {

	/**
	 * @type {SystemLogger}
	 */
	this.logger = SystemLogger.getLogger ( "FieldGroupBinding" );
}

/**
 * Identifies binding.
 */
FieldGroupBinding.prototype.toString = function () {

	return "[FieldGroupBinding]";
}

/**
 * Notice that we need to do this on register already!
 * @overloads {Binding#onBindingRegister}
 */
FieldGroupBinding.prototype.onBindingRegister = function () {

	FieldGroupBinding.superclass.onBindingRegister.call ( this );
	this.propertyMethodMap [ "label" ] = this.setLabel;
	this._buildDOMContent ();
}

/**
 * Build DOM content.
 */
FieldGroupBinding.prototype._buildDOMContent = function () {

	//this.shadowTree.fieldset = DOMUtil.createElementNS(Constants.NS_XHTML, "fieldset", this.bindingDocument);
	//this.shadowTree.legend = DOMUtil.createElementNS(Constants.NS_XHTML, "legend", this.bindingDocument);
	//this.shadowTree.fieldset.appendChild(this.shadowTree.legend);
	//while (this.bindingElement.firstChild) {
	//	this.shadowTree.fieldset.appendChild(this.bindingElement.firstChild);
	//}
	//this.bindingElement.appendChild(this.shadowTree.fieldset);

	var label = this.getProperty ( "label" );
	if ( label ) {
		this.setLabel ( label );
	} else {
		this.attachClassName ( FieldGroupBinding.CLASSNAME_NOLABEL );
	}
}

/**
 * Set label.
 * @parm {string} label
 */
FieldGroupBinding.prototype.setLabel = function ( label ) {
	
	this.setProperty ( "label", label );
	
	if ( this.shadowTree.labelBinding == null ) {
	
		var labelBinding = LabelBinding.newInstance ( this.bindingDocument );
		var cell = this.shadowTree [ FieldGroupBinding.NORTH ];
		labelBinding.attachClassName ( "fieldgrouplabel" );
		this.bindingElement.insertBefore(labelBinding.bindingElement, this.bindingElement.firstChild);
		labelBinding.attach ();
		this.shadowTree.labelBinding = labelBinding;

		this.shadowTree.labelBinding.bindingElement.appendChild(DOMUtil.createElementNS(Constants.NS_XHTML, "div", this.bindingDocument));
	}
	
	this.shadowTree.labelBinding.setLabel ( 
		Resolver.resolve ( label )
	);
}

/** 
 * Get label.
 * @return {string}
 */
FieldGroupBinding.prototype.getLabel = function () {
	
	return this.getProperty ( "label" );
}

/**
 * Make sure that added content is placed in matrix center.
 * @overwrites {Binding#add}  
 * @param {Binding} binding
 * @return {Binding}
 */
FieldGroupBinding.prototype.add = function ( binding ) {

	this.shadowTree [ FieldGroupBinding.CENTER ].appendChild ( binding.bindingElement );
	return binding;
}

/**
 * Make sure that added content is placed in matrix center.
 * @overwrites {Binding#addFirst}
 * @param {Binding} binding
 * @return {Binding}
 */
FieldGroupBinding.prototype.addFirst = function ( binding ) {
	
	var centerCell = this.shadowTree [ FieldGroupBinding.CENTER ];
	centerCell.insertBefore ( binding.bindingElement, centerCell.firstChild );
	return binding;
}