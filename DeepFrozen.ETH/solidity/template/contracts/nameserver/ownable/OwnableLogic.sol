// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./OwnableLayout.sol";
import "./OwnableInterface.sol";

contract OwnableLogic is OwnableLayout, OwnableInterface {

    /**
     * @dev Throws if called by any account other than the owner.
     */
    modifier onlyOwner() {
        require(owner() == msg.sender, "Ownable: caller is not the owner");
        _;
    }

    modifier onlyAuth() {
        require(owner() == msg.sender || _associatedOperators[msg.sender], "Ownable: caller is un-authed");
        _;
    }

    /**
     * @dev Returns the address of the current owner.
     */
    function owner() override public view returns (address) {
        return _owner;
    }

    /**
     * @dev Leaves the contract without owner. It will not be possible to call
     * `onlyOwner` functions anymore. Can only be called by the current owner.
     *
     * NOTE: Renouncing ownership will leave the contract without an owner,
     * thereby removing any functionality that is only available to the owner.
     */
    function renounceOwnership() override public onlyOwner {
        emit OwnershipTransferred(_owner, address(0));
        _owner = address(0);
    }

    /**
     * @dev Transfers ownership of the contract to a new account (`newOwner`).
     * Can only be called by the current owner.
     */
    function transferOwnership(address newOwner) override public onlyOwner {
        require(newOwner != address(0), "Ownable: new owner is the zero address");
        emit OwnershipTransferred(_owner, newOwner);
        _owner = newOwner;
    }

    function setAssociatedOperator(address newAssociatedOperator, bool flag) override public onlyOwner {
        require(newAssociatedOperator != address(0), "Ownable: newAssociatedOperator is the zero address");

        _associatedOperators[newAssociatedOperator] = flag;
    }

}
